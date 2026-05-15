# Azure Key Vault Migration Plan

## Purpose

This document defines the plan for migrating Finance Tracker secrets from local configuration files and general environment-variable fallback into Azure Key Vault.

The goal is a tighter, easier-to-understand security model:
- Azure Key Vault is the only runtime source for production secrets.
- startup fails fast when a required secret is missing from Key Vault.
- credential selection is deterministic and does not use broad fallback chains.
- local files may contain only non-secret developer settings.
- future contributors can understand exactly where secrets live and how the app receives them.

## Target State

At runtime, the application should load required secrets from one configured Azure Key Vault before registering services that depend on those secrets.

Required runtime secrets:
- `Auth:JwtSecret`
- `ConnectionStrings:Default`
- `Supabase:Url`
- `Supabase:AnonKey`
- `Redis:ConnectionString`, if Redis is required in the target environment

Optional runtime settings that may remain outside Key Vault because they are not secrets:
- `Auth:AccessTokenMinutes`
- `Redis:InstanceName`
- `DashboardCache:Enabled`
- `DashboardCache:TtlSeconds`
- `DashboardCache:VersionTtlHours`
- `Database:CommandTimeoutSeconds`
- `GraphQL:ExecutionTimeoutSeconds`
- logging levels
- allowed hosts

The app should not silently fall back to `appsettings.Development.json`, developer machine environment variables, Visual Studio credentials, or other ambient identity sources when a required Key Vault secret is missing.

## Secret Naming

Use Azure Key Vault secret names that map cleanly to ASP.NET Core configuration keys.

Azure Key Vault secret names cannot contain `:`, so use the standard double-dash convention:

```text
Auth--JwtSecret
ConnectionStrings--Default
Supabase--Url
Supabase--AnonKey
Redis--ConnectionString
```

The Azure Key Vault configuration provider maps `--` to `:` so the application still reads:

```text
Auth:JwtSecret
ConnectionStrings:Default
Supabase:Url
Supabase:AnonKey
Redis:ConnectionString
```

If migration scripts need a higher-privilege or separate connection string, use:

```text
ConnectionStrings--Migration
```

Do not store multiple environment variants in one vault using ambiguous names. Prefer one vault per environment, for example:
- `kv-fintrack-dev-neu`
- `kv-finance-tracker-staging`
- `kv-finance-tracker-prod`

## Deterministic Credential Policy

Do not use `DefaultAzureCredential` in the application runtime path.

Why:
- it tries multiple credential sources
- the winning source can differ by machine or host
- it can accidentally authenticate with local developer credentials
- it makes production identity issues harder to reason about

Use explicit credentials instead:

- Production and hosted environments: `ManagedIdentityCredential`
- Local Key Vault development, only when intentionally enabled: `AzureCliCredential`

The credential mode must be explicit. Recommended bootstrap configuration:

```text
AzureKeyVault:VaultUri
AzureKeyVault:CredentialMode
AzureKeyVault:ManagedIdentityClientId
```

Accepted values:
- `ManagedIdentity`
- `AzureCli`

Rules:
- `ManagedIdentity` is the only allowed mode outside development.
- `AzureCli` is allowed only for local development and manual verification.
- no chained credential fallback is allowed.
- missing identity configuration must fail startup.

## Bootstrap Configuration

The application needs a tiny amount of non-secret bootstrap configuration before it can contact Key Vault.

Allowed bootstrap sources:
- deployment environment variables for non-secret values
- non-secret `appsettings.json`
- optional local non-secret developer config

Allowed bootstrap keys:

```text
AzureKeyVault:VaultUri
AzureKeyVault:CredentialMode
AzureKeyVault:ManagedIdentityClientId
ASPNETCORE_ENVIRONMENT
```

No application secret should be accepted from bootstrap configuration.

Recommended environment variable equivalents:

```bash
AzureKeyVault__VaultUri="https://kv-finance-tracker-prod.vault.azure.net/"
AzureKeyVault__CredentialMode="ManagedIdentity"
AzureKeyVault__ManagedIdentityClientId="<user-assigned-managed-identity-client-id-if-used>"
```

## Startup Flow

Update startup to follow this order:

1. Build minimal bootstrap configuration.
2. Read `AzureKeyVault:VaultUri`.
3. Read explicit `AzureKeyVault:CredentialMode`.
4. Create the matching Azure credential.
5. Create a `SecretClient`.
6. Validate that every required secret exists, is enabled, and is non-empty.
7. Add Azure Key Vault as a configuration provider.
8. Register app services.
9. Run existing auth, database, Supabase, cache, repository, service, and GraphQL setup.

The important behavior is step 6. It prevents this bad state:

```text
Key Vault secret missing
local environment variable exists
application keeps running with local value
```

The app must fail before service registration if Key Vault does not contain a required production secret.

## Required Dependencies

Add these package references to [FinanceTracker.csproj](/workspaces/vela/FinanceTracker.csproj):

```xml
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="<pin-current-stable>" />
<PackageReference Include="Azure.Identity" Version="<pin-current-stable>" />
```

Notes:
- `Azure.Extensions.AspNetCore.Configuration.Secrets` provides the ASP.NET Core configuration provider for Key Vault.
- `Azure.Identity` provides explicit credential types such as `ManagedIdentityCredential` and `AzureCliCredential`.
- Pin versions deliberately during implementation and record the choice in the PR.
- Do not add `Azure.Security.KeyVault.Secrets` directly unless the implementation needs direct `SecretClient` APIs not already available transitively. If direct validation uses `SecretClient`, adding it explicitly is acceptable for clarity.

## Azure Setup Plan

Create a vault per environment.

Example CLI flow for a development vault:

```bash
az group create \
  --name rg-finance-tracker-dev-neu \
  --location northeurope

az keyvault create \
  --name kv-fintrack-dev-neu \
  --resource-group rg-finance-tracker-dev-neu \
  --location northeurope \
  --enable-rbac-authorization true
```

Create or assign a managed identity for hosted production runtime:

```bash
az identity create \
  --name mi-finance-tracker-prod \
  --resource-group rg-finance-tracker-prod \
  --location northeurope
```

Grant the runtime identity read access to secrets:

```bash
az role assignment create \
  --assignee "<managed-identity-principal-id>" \
  --role "Key Vault Secrets User" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.KeyVault/vaults/<vault-name>"
```

Set secrets:

```bash
az keyvault secret set --vault-name kv-fintrack-dev-neu --name Auth--JwtSecret --value "<strong-random-secret>"
az keyvault secret set --vault-name kv-fintrack-dev-neu --name ConnectionStrings--Default --value "<postgres-connection-string>"
az keyvault secret set --vault-name kv-fintrack-dev-neu --name Supabase--Url --value "<supabase-url>"
az keyvault secret set --vault-name kv-fintrack-dev-neu --name Supabase--AnonKey --value "<supabase-anon-key>"
az keyvault secret set --vault-name kv-fintrack-dev-neu --name Redis--ConnectionString --value "<redis-connection-string>"
```

For local development using Azure CLI authentication, grant the signed-in developer identity only to the development vault:

```bash
az role assignment create \
  --assignee "$(az ad signed-in-user show --query id -o tsv)" \
  --role "Key Vault Secrets User" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/rg-finance-tracker-dev-neu/providers/Microsoft.KeyVault/vaults/kv-fintrack-dev-neu"
```

Do not grant local developer identities access to production vaults for normal development.

## Files To Change

### [FinanceTracker.csproj](/workspaces/vela/FinanceTracker.csproj)

Add Azure Key Vault and Azure Identity dependencies.

### [Program.cs](/workspaces/vela/Program.cs)

Load Key Vault secrets before these existing calls:
- `AddApiAuth`
- `AddSupabaseClient`
- `AddDatabase`
- `AddDashboardCaching`

Recommended shape:

```csharp
builder.Configuration.AddFinanceTrackerKeyVault(builder.Environment);
builder.Configuration.ValidateRequiredKeyVaultSecrets(builder.Environment);
```

The final implementation can use one extension method if that keeps startup cleaner.

### New: `Api/Extensions/KeyVaultExtensions.cs`

Create a single extension that owns:
- bootstrap key reading
- credential selection
- Key Vault provider registration
- required secret validation
- clear startup errors

This keeps `Program.cs` readable and makes the security path discoverable.

### New: `Infrastructure/Security/RequiredSecretOptions.cs`

Optional, but recommended if the required-secret list grows.

Own:
- required secret names
- environment-specific required secrets
- mapping from Key Vault names to configuration keys

### [Api/Extensions/AuthExtensions.cs](/workspaces/vela/Api/Extensions/AuthExtensions.cs)

Add hard validation for `Auth:JwtSecret`:
- required
- not a placeholder
- at least 32 bytes after decoding or UTF-8 conversion

The current README documents this rule, but startup should enforce it.

### [Infrastructure/Auth/ApiTokenService.cs](/workspaces/vela/Infrastructure/Auth/ApiTokenService.cs)

Stop duplicating JWT secret parsing logic.

Move shared JWT key parsing and validation into one helper, for example:
- `Infrastructure/Auth/JwtSigningKeyFactory.cs`

Both token creation and token validation should use the same validation behavior.

### [appsettings.json](/workspaces/vela/appsettings.json)

Remove secret-shaped placeholders.

Keep only non-secret defaults and bootstrap metadata, for example:

```json
{
  "AzureKeyVault": {
    "VaultUri": "",
    "CredentialMode": "ManagedIdentity",
    "ManagedIdentityClientId": ""
  },
  "Auth": {
    "AccessTokenMinutes": 60
  }
}
```

Do not keep these placeholders:
- `Auth:JwtSecret`
- `ConnectionStrings:Default`
- `Supabase:AnonKey`
- `Redis:ConnectionString`

Keeping placeholders makes it less obvious that Key Vault is the source of truth.

### `appsettings.Development.json`

Do not use this file for secrets after the migration.

Because this file is ignored, each developer may have a local copy today. The migration should make the application ignore secret values in this file by requiring Key Vault validation for required secrets.

Optional allowed local values:
- local ports
- logging verbosity
- local non-secret feature flags
- `AzureKeyVault:VaultUri` for the development vault
- `AzureKeyVault:CredentialMode` set to `AzureCli`

### [.gitignore](/workspaces/vela/.gitignore)

Keep local settings ignored.

Add comments clarifying:
- local appsettings files are non-secret after Key Vault migration
- secrets must live in Azure Key Vault
- no `.env` file containing real secrets should be committed

### [README.md](/workspaces/vela/README.md)

Replace the current local-secret setup instructions with:
- Key Vault setup
- required bootstrap config
- local development with Azure CLI credential
- production with managed identity
- secret names and examples

### [docs/test-requirements.md](/workspaces/vela/docs/test-requirements.md)

Update environment requirements:
- unit tests do not need Key Vault
- integration tests should use test-only secrets or container-provided values
- production Key Vault must never be used by automated tests

### [docs/e2e-quickstart.md](/workspaces/vela/docs/e2e-quickstart.md)

Update E2E startup instructions:
- local test JWTs can still use test-only config
- production-like E2E should load `Auth:JwtSecret` from a test Key Vault
- never point E2E tests at production Key Vault

### [docs/devcontainer-plan.md](/workspaces/vela/docs/devcontainer-plan.md)

Remove the requirement that a developer must prepare `appsettings.Development.json` with real credentials before opening the devcontainer.

Replace it with:
- developer must have Azure CLI available on the host or in the devcontainer
- developer must have access to the development Key Vault
- local bootstrap config points to the development vault

### [scripts/devcontainer/validate-env.sh](/workspaces/vela/scripts/devcontainer/validate-env.sh)

Stop requiring `appsettings.Development.json` with real credentials.

Validate instead:
- `az` exists
- `az account show` succeeds when local Key Vault mode is enabled
- `AzureKeyVault:VaultUri` or equivalent bootstrap variable exists

### [scripts/devcontainer/ensure-dev-db.sh](/workspaces/vela/scripts/devcontainer/ensure-dev-db.sh)

Stop reading database credentials from `appsettings.Development.json`.

Read the development database connection string from Key Vault or accept an explicit non-production override for disposable local containers.

### [scripts/apply-migrations.sh](/workspaces/vela/scripts/apply-migrations.sh)

Add Key Vault support for migration connection strings.

Recommended order:
1. explicit `--connection-uri` for one-off controlled execution
2. `ConnectionStrings--Migration` from the configured vault
3. `ConnectionStrings--Default` from the configured vault
4. fail

Do not fall back to `appsettings.Development.json`.

### [scripts/validate-migrations.sh](/workspaces/vela/scripts/validate-migrations.sh)

Mirror the migration connection-string lookup used by `apply-migrations.sh`.

### [scripts/rollback-migrations.sh](/workspaces/vela/scripts/rollback-migrations.sh)

Mirror the migration connection-string lookup used by `apply-migrations.sh`.

### New: `scripts/keyvault/set-dev-secrets.sh`

Optional helper for onboarding.

Responsibilities:
- verify Azure CLI login
- verify target vault
- set required development secrets
- refuse production vault names unless an explicit confirmation flag is present

### New: `scripts/keyvault/verify-secrets.sh`

Optional helper for CI and onboarding.

Responsibilities:
- list required secret names
- verify they exist in the selected vault
- verify they are enabled
- never print secret values

## Files To Remove Or Stop Depending On

The following should not remain part of the supported secret path:

- local real-secret `appsettings.Development.json`
- docs that instruct developers to place real Supabase, Postgres, Redis, or JWT secrets in local JSON
- migration scripts that silently read real credentials from local JSON
- any sample file containing realistic passwords, tokens, or connection strings
- duplicate JWT secret parsing logic

Because `appsettings.Development.json` is ignored and local to each developer, the repository cannot delete every copy. The migration should instead make those local secret values irrelevant by enforcing Key Vault presence.

## Security Rules

Required production rules:

- The app must fail startup if `AzureKeyVault:VaultUri` is missing.
- The app must fail startup if `AzureKeyVault:CredentialMode` is missing.
- The app must fail startup if production uses `AzureCli`.
- The app must fail startup if a required Key Vault secret is missing, disabled, or empty.
- The app must fail startup if `Auth:JwtSecret` is shorter than the required length.
- The app must not log secret values.
- The app must not log connection strings.
- The app must not expose Key Vault exception details through GraphQL responses.

Recommended operational rules:

- enable Key Vault diagnostic logging
- enable soft delete and purge protection
- use RBAC instead of access policies for new vaults
- grant the app only `Key Vault Secrets User`
- grant operators only the minimum roles needed
- use a separate vault per environment
- rotate `Auth:JwtSecret` through a planned token invalidation window until multi-key JWT validation exists

## Test Plan

### Unit Tests

Add tests for:
- credential mode selection rejects unknown values
- production rejects `AzureCli`
- required secret list contains all current runtime secrets
- JWT secret validation rejects missing values
- JWT secret validation rejects placeholders
- JWT secret validation rejects weak values
- JWT secret validation accepts strong UTF-8 and base64 values

### Integration Tests

Avoid requiring real Azure access in normal test runs.

Use one of these approaches:
- unit-test the Key Vault extension around small abstractions
- mock a `IRequiredSecretValidator`
- run optional Azure-backed tests only when explicit environment flags are set

Optional Azure-backed test flags:

```bash
FINANCE_TRACKER_ENABLE_AZURE_TESTS=true
AzureKeyVault__VaultUri="https://kv-finance-tracker-test.vault.azure.net/"
AzureKeyVault__CredentialMode="AzureCli"
```

These tests must never run against production.

### Manual Verification

Run:

```bash
az account show
az keyvault secret show --vault-name kv-fintrack-dev-neu --name Auth--JwtSecret --query "attributes.enabled"
dotnet run
curl -s http://localhost:8080/health
```

Then temporarily disable or rename one required secret and verify that startup fails before the API starts.

## Rollout Plan

### Phase 1: Inventory And Vault Setup

- create development, staging, and production vaults
- decide exact vault names
- decide managed identity strategy
- add required secrets to development vault
- verify the app team can read only the development vault

### Phase 2: Add Application Key Vault Loading

- add Azure packages
- add Key Vault startup extension
- add required secret validation
- centralize JWT secret validation
- update `Program.cs`

### Phase 3: Remove Local Secret Dependency

- remove secret placeholders from `appsettings.json`
- update README and docs
- update devcontainer validation
- update migration scripts
- add Key Vault helper scripts if useful

### Phase 4: Test And Harden

- add unit tests for validation and startup policy
- run existing unit tests
- run integration tests
- run manual missing-secret startup test
- verify no secret values appear in logs

### Phase 5: Production Deployment

- assign managed identity to the hosted app
- grant Key Vault secret-read permission
- set production bootstrap config
- deploy with `AzureKeyVault:CredentialMode=ManagedIdentity`
- verify startup and health
- remove any production environment variables containing old secrets

## Success Criteria

- no committed file contains real secrets
- production secrets live only in Azure Key Vault
- production runtime uses managed identity only
- local development can use a development vault intentionally through Azure CLI
- missing Key Vault secrets fail startup
- local environment variables cannot silently replace missing Key Vault secrets
- migration scripts no longer depend on local real-secret JSON
- documentation teaches the new model clearly
