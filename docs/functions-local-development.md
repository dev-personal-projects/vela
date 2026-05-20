# Azure Functions Local Development

## Prerequisites

- .NET 10 SDK
- Azure Functions Core Tools v4
- Azure CLI authenticated for Key Vault access when using the development vault
- Azurite or real Azure Storage for `AzureWebJobsStorage`

The devcontainer installs these tools and starts Azurite automatically.

## Setup

Create local settings from the template:

```bash
cp local.settings.json.template local.settings.json
```

The template uses:

```text
AzureWebJobsStorage=UseDevelopmentStorage=true
AzureKeyVault__CredentialMode=AzureCli
AzureKeyVault__VaultUri=https://kv-vela-dev-neu.vault.azure.net/
Auth__JwtSecret=vela-local-dev-jwt-secret-keep-out-of-git-32-bytes
ConnectionStrings__Default=Host=localhost;Port=5432;Database=vela_dev;Username=postgres;Password=postgres
Supabase__Url=https://localhost
Supabase__AnonKey=vela-local-dev-supabase-anon-key
```

In `Development`, Vela first tries Key Vault. If the development vault is missing, torn down, or unreachable, startup falls back to the local values above. Update them in ignored `local.settings.json` for your own machine. Deployed environments still require Key Vault and reject bootstrap secrets.

Authenticate if you want to use the development vault:

```bash
az login --tenant 7d20ca84-8064-4008-9ddb-bf9216c87c7b
az account set --subscription 9b93ac7e-4e02-4852-90e9-3b5fab34c960
```

Run:

```bash
func start --port 7071
```

Verify:

```bash
curl -fsS http://localhost:7071/api/v1/health
```
