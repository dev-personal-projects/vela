# Azure Functions Local Development

## Prerequisites

- .NET 10 SDK
- Azure Functions Core Tools v4
- Azure CLI authenticated for Key Vault access
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
AzureKeyVault__VaultUri=https://kv-fintrack-dev-neu.vault.azure.net/
```

Authenticate:

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
