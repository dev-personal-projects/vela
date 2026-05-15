# Vela Azure Functions

This folder contains the Azure Functions cutover scripts for the production REST API.

## Deploy

```bash
./Infrastructure/Azure/AzureFunctions/1.deploy-financetracker-AzFunc.sh
```

Preview:

```bash
./Infrastructure/Azure/AzureFunctions/1.deploy-financetracker-AzFunc.sh --plan
```

## Update

```bash
./Infrastructure/Azure/AzureFunctions/3.update-financetracker-AzFunc.sh
```

## Teardown

```bash
./Infrastructure/Azure/AzureFunctions/2.teardown-financetracker-AzFunc.sh --yes --include-storage --include-github-automation
```

The Function App uses:

- .NET isolated worker
- Azure Functions v4
- Flex Consumption
- system-assigned managed identity
- Key Vault RBAC via `Key Vault Secrets User`
- GitHub Actions OIDC deployment
