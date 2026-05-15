# Finance Tracker Azure Deployment

Production now runs as a .NET isolated Azure Function App on Flex Consumption.

The previous Azure Container Apps deployment is preserved only for rollback until the cutover is verified, then removed with the teardown script.

## Active Path

| File | Purpose |
| --- | --- |
| `globalenv.config` | Non-secret Azure names and deployment identifiers. |
| `AzureFunctions/1.deploy-financetracker-AzFunc.sh` | Creates/updates Function App infrastructure, app settings, identity, Key Vault RBAC, and GitHub OIDC secrets. |
| `AzureFunctions/2.teardown-financetracker-AzFunc.sh` | Removes Function App resources if rollback or cleanup is needed. |
| `AzureFunctions/3.update-financetracker-AzFunc.sh` | Restarts and health-checks the Function App. |

## Legacy Path

| File | Purpose |
| --- | --- |
| `AzureContainers/1.deploy-financetracker-AzCont.sh` | Legacy Container Apps deployment. |
| `AzureContainers/2.teardown-financetracker-AzCont.sh` | Removes old Container Apps resources after Function App cutover. |
| `AzureContainers/3.update-financetracker-AzCont.sh` | Legacy Container Apps update script. |

## Required Key Vault Secrets

| Key Vault secret | App configuration key |
| --- | --- |
| `Auth--JwtSecret` | `Auth:JwtSecret` |
| `ConnectionStrings--Default` | `ConnectionStrings:Default` |
| `Supabase--Url` | `Supabase:Url` |
| `Supabase--AnonKey` | `Supabase:AnonKey` |
| `Redis--ConnectionString` | `Redis:ConnectionString` |

## Deploy Function App

```bash
./Infrastructure/Azure/AzureFunctions/1.deploy-financetracker-AzFunc.sh
```

Then push `main` or manually run:

```text
.github/workflows/dev-deploy-fintrack-api-functions.yml
```

## Verify

```bash
curl -fsS https://func-fintrack-api-dev-neu-001.azurewebsites.net/api/v1/health
```

## Teardown Old Container Apps

After Function App verification:

```bash
./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh \
  --yes \
  --include-environment \
  --include-acr \
  --include-github-automation
```

Keep Key Vault and monitoring because the Function App uses them.
