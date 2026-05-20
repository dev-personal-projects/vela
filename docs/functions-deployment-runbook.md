# Azure Functions Deployment Runbook

## Deploy

Provision or update the Function App:

```bash
./Infrastructure/Azure/AzureFunctions/1.deploy-vela-AzFunc.sh
```

Preview only:

```bash
./Infrastructure/Azure/AzureFunctions/1.deploy-vela-AzFunc.sh --plan
```

## Validate

```bash
az functionapp show \
  --name func-vela-api-dev-neu-001 \
  --resource-group rg-vela-dev-neu \
  --query '{state:state,hostNames:defaultHostName}' \
  -o json

curl -fsS "https://func-vela-api-dev-neu-001.azurewebsites.net/api/v1/health"
```

## Cutover

1. Confirm GitHub Actions deploy workflow is green.
2. Confirm `/api/v1/health` returns `200`.
3. Confirm sign-in works.
4. Confirm one protected endpoint works.
5. Update client base URL to the Function App.
