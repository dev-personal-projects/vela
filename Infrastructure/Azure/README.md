# Finance Tracker Azure Deployment

This folder contains the Azure deployment automation for the Finance Tracker GraphQL API.

The production-style runtime path is:

1. Build the .NET API with the root `Dockerfile`.
2. Deploy to Azure Container Apps with `az containerapp up`.
3. Let Azure create the GitHub Actions workflow for future deployments.
4. Send Container Apps logs to Log Analytics and application telemetry to Application Insights.
5. Load application secrets from Azure Key Vault at runtime through managed identity.

Application secrets are not stored in these scripts, GitHub repository secrets, Docker images, or appsettings files.

## Files

| File | Purpose |
| --- | --- |
| `globalenv.config` | Non-secret Azure names, resource identifiers, scale settings, and GitHub deployment settings. |
| `AzureContainers/1.deploy-financetracker-AzCont.sh` | Creates or updates the Container Apps infrastructure and runs `az containerapp up`. |
| `AzureContainers/2.teardown-financetracker-AzCont.sh` | Safely removes Container Apps resources. Destructive resources require explicit flags. |
| `AzureContainers/3.update-financetracker-AzCont.sh` | Refreshes runtime settings, identity access, scaling, health checks, and optional workflow/image updates. |

## Required Azure Resources

The deployment expects these configured names from `globalenv.config`:

| Resource | Config key |
| --- | --- |
| Resource group | `PROJECT_RESOURCE_GROUP` |
| Azure Container Registry | `ACR_NAME` |
| Container Apps environment | `CONTAINER_APP_ENV_NAME` |
| Container App | `CONTAINER_APP_NAME` |
| Log Analytics workspace | `LOG_ANALYTICS_WORKSPACE_NAME` |
| Application Insights | `APPLICATION_INSIGHTS_NAME` |
| Azure Key Vault | `KEY_VAULT_NAME` |

The configured location is always `northeurope`. The scripts fail if `PROJECT_LOCATION`, `EUROPE_LOCATION`, or `EUROPE_PREFIX` drift away from North Europe.

## Naming Standard

Container App resources use a compact resource-type/workload/environment/region/instance pattern:

| Resource | Name |
| --- | --- |
| Container App | `ca-fintrack-api-dev-neu-001` |
| Container Apps environment | `cae-fintrack-api-dev-neu-001` |
| Azure Container Registry | `crfintrackapidevneu001` |
| Log Analytics workspace | `log-fintrack-api-dev-neu-001` |
| Application Insights | `appi-fintrack-api-dev-neu-001` |

The existing resource group and Key Vault names are kept stable to avoid unnecessary secret migration.

## Required Key Vault Secrets

These secrets must exist in Azure Key Vault before deployment:

| Key Vault secret | App configuration key |
| --- | --- |
| `Auth--JwtSecret` | `Auth:JwtSecret` |
| `ConnectionStrings--Default` | `ConnectionStrings:Default` |
| `Supabase--Url` | `Supabase:Url` |
| `Supabase--AnonKey` | `Supabase:AnonKey` |
| `Redis--ConnectionString` | `Redis:ConnectionString` |

The app fails fast if any required secret is missing, empty, disabled, or supplied from bootstrap configuration instead of Key Vault.

## First Deployment

Run the deploy script only after the `Dockerfile` has been merged or pushed to the branch configured by `GITHUB_BRANCH`.

```bash
./Infrastructure/Azure/AzureContainers/1.deploy-financetracker-AzCont.sh
```

The deploy script:

- Verifies Azure CLI and GitHub CLI authentication.
- Validates the configured subscription.
- Confirms the remote GitHub branch contains the Dockerfile.
- Confirms the required Key Vault secrets exist.
- Creates the resource group, ACR, Log Analytics workspace, Application Insights, and Container Apps environment when needed.
- Connects Container Apps environment logs to Log Analytics.
- Creates a deployment service principal for GitHub Actions.
- Stores deployment-only credentials as GitHub repository secrets.
- Runs `az containerapp up` so Azure creates the GitHub Actions workflow.
- Assigns managed identity to the Container App.
- Grants that managed identity Key Vault Secrets User.
- Sets `APPLICATIONINSIGHTS_CONNECTION_STRING` and `OTEL_SERVICE_NAME` on the Container App.
- Refreshes runtime settings and waits for `/health`.

## Monitoring

The deployment creates workspace-based Application Insights backed by the configured Log Analytics workspace.

| Signal | Destination |
| --- | --- |
| Container stdout/stderr logs | Log Analytics via Container Apps environment logging |
| Container Apps platform logs | Log Analytics |
| ASP.NET Core requests, dependencies, exceptions, traces, and metrics | Application Insights through Azure Monitor OpenTelemetry |

The app only enables Azure Monitor telemetry when `APPLICATIONINSIGHTS_CONNECTION_STRING` is present, which the Azure scripts set during deployment and update.

## Updating

Preview the update plan:

```bash
./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh --plan
```

Refresh runtime settings, managed identity access, observability settings, scaling, and health:

```bash
./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh
```

Useful options:

```bash
./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh --restart --logs
./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh --image crfintrackapidevneu001.azurecr.io/app:tag
./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh --trigger-workflow --workflow <workflow-file.yml>
```

## Teardown

Preview what would be deleted:

```bash
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --plan
```

Default teardown deletes only the Container App:

```bash
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh
```

More destructive cleanup requires explicit flags:

```bash
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --include-environment
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --include-acr
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --include-monitoring
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --include-keyvault
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --include-github-automation
```

Deleting the whole resource group is available, but should be rare:

```bash
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --delete-resource-group
```

Production teardown is blocked unless `--delete-production` is supplied.

## Local Validation

Run these checks before publishing changes:

```bash
for script in Infrastructure/Azure/AzureContainers/*.sh; do bash -n "$script"; done
git diff --check
dotnet build FinanceTracker.csproj
docker build -t finance-tracker-api:local .
```

Use `--plan` on update and teardown scripts before any real Azure operation.
