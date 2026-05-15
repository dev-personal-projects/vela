#!/usr/bin/env bash
set -euo pipefail

declare -r RED='\033[0;31m'
declare -r GREEN='\033[0;32m'
declare -r YELLOW='\033[0;33m'
declare -r BLUE='\033[0;34m'
declare -r NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]    $*${NC}"; }
log_success() { echo -e "${GREEN}[SUCCESS] $*${NC}"; }
log_warning() { echo -e "${YELLOW}[WARNING] $*${NC}"; }
log_error()   { echo -e "${RED}[ERROR]   $*${NC}" >&2; }

PLAN_ONLY=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --plan) PLAN_ONLY=true; shift ;;
    -h|--help)
      echo "Usage: $0 [--plan]"
      exit 0
      ;;
    *) log_error "Unknown option: $1"; exit 1 ;;
  esac
done

load_config() {
  local dir
  dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
  while [[ "$dir" != "/" ]]; do
    if [[ -f "$dir/globalenv.config" ]]; then
      # shellcheck source=/dev/null
      source "$dir/globalenv.config"
      log_info "Loaded config from $dir/globalenv.config"
      return 0
    fi
    dir="$(dirname "$dir")"
  done
  log_error "globalenv.config not found"
  exit 1
}

validate_config() {
  local required=(
    ENVIRONMENT_PREFIX PROJECT_PREFIX PROJECT_LOCATION PROJECT_RESOURCE_GROUP
    PROJECT_SUBSCRIPTION_ID TENANT_ID KEY_VAULT_NAME KEY_VAULT_URI GITHUB_REPO
    GITHUB_BRANCH LOG_ANALYTICS_WORKSPACE_NAME APPLICATION_INSIGHTS_NAME
    FUNCTION_APP_NAME FUNCTION_STORAGE_ACCOUNT_NAME FUNCTIONS_RUNTIME
    FUNCTIONS_RUNTIME_VERSION FUNCTIONS_VERSION
  )

  for var in "${required[@]}"; do
    if [[ -z "${!var:-}" ]]; then
      log_error "Required variable '$var' is not set in globalenv.config"
      exit 1
    fi
  done

  if [[ ! "$FUNCTION_STORAGE_ACCOUNT_NAME" =~ ^[a-z0-9]{3,24}$ ]]; then
    log_error "FUNCTION_STORAGE_ACCOUNT_NAME must be 3-24 lowercase alphanumeric characters."
    exit 1
  fi
}

repo_root() {
  cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd
}

repo_short() {
  echo "$GITHUB_REPO" | sed -E 's|^https://github.com/||; s|/$||'
}

print_plan() {
  cat <<PLAN

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Finance Tracker Azure Functions Deployment Plan
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Environment       : $ENVIRONMENT_PREFIX
Resource Group    : $PROJECT_RESOURCE_GROUP
Location          : $PROJECT_LOCATION
Function App      : $FUNCTION_APP_NAME
Runtime           : $FUNCTIONS_RUNTIME $FUNCTIONS_RUNTIME_VERSION
Functions Version : $FUNCTIONS_VERSION
Storage Account   : $FUNCTION_STORAGE_ACCOUNT_NAME
Key Vault         : $KEY_VAULT_NAME
App Insights      : $APPLICATION_INSIGHTS_NAME
GitHub Repo       : $(repo_short)
GitHub Branch     : $GITHUB_BRANCH
Workflow          : .github/workflows/dev-deploy-fintrack-api-functions.yml
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PLAN
}

ensure_tools() {
  command -v az >/dev/null 2>&1 || { log_error "Azure CLI is required."; exit 1; }
  command -v gh >/dev/null 2>&1 || { log_error "GitHub CLI is required."; exit 1; }
  gh auth status --hostname github.com >/dev/null 2>&1 || {
    log_error "GitHub CLI must be authenticated. Run: gh auth login --hostname github.com"
    exit 1
  }
}

setup_azure() {
  log_info "Verifying Azure CLI authentication"
  if ! az account show >/dev/null 2>&1; then
    az login --tenant "$TENANT_ID"
  fi

  az account set --subscription "$PROJECT_SUBSCRIPTION_ID"
  local active
  active=$(az account show --query id -o tsv)
  [[ "$active" == "$PROJECT_SUBSCRIPTION_ID" ]] || {
    log_error "Subscription mismatch. Active: $active Expected: $PROJECT_SUBSCRIPTION_ID"
    exit 1
  }
  log_success "Azure subscription: $PROJECT_SUBSCRIPTION_ID"
}

ensure_providers() {
  log_info "Registering required Azure resource providers"
  az provider register --namespace Microsoft.Web --wait --output none
  az provider register --namespace Microsoft.Storage --wait --output none
  az provider register --namespace Microsoft.ManagedIdentity --wait --output none
  log_success "Required Azure resource providers are registered"
}

ensure_resource_group() {
  if ! az group show --name "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_info "Creating resource group: $PROJECT_RESOURCE_GROUP"
    az group create --name "$PROJECT_RESOURCE_GROUP" --location "$PROJECT_LOCATION" --output none
  fi
  log_success "Resource group ready: $PROJECT_RESOURCE_GROUP"
}

ensure_storage() {
  if ! az storage account show \
      --name "$FUNCTION_STORAGE_ACCOUNT_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_info "Creating Function runtime storage account: $FUNCTION_STORAGE_ACCOUNT_NAME"
    az storage account create \
      --name "$FUNCTION_STORAGE_ACCOUNT_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --location "$PROJECT_LOCATION" \
      --sku Standard_LRS \
      --kind StorageV2 \
      --https-only true \
      --min-tls-version TLS1_2 \
      --output none
  fi

  FUNCTION_STORAGE_CONNECTION_STRING=$(az storage account show-connection-string \
    --name "$FUNCTION_STORAGE_ACCOUNT_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query connectionString \
    -o tsv)

  log_success "Function storage ready: $FUNCTION_STORAGE_ACCOUNT_NAME"
}

ensure_observability_values() {
  APPLICATIONINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show \
    --app "$APPLICATION_INSIGHTS_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query connectionString \
    -o tsv)

  [[ -n "$APPLICATIONINSIGHTS_CONNECTION_STRING" ]] || {
    log_error "Application Insights connection string not found for $APPLICATION_INSIGHTS_NAME"
    exit 1
  }
}

ensure_function_app() {
  if ! az functionapp show \
      --name "$FUNCTION_APP_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_info "Creating Function App on Flex Consumption: $FUNCTION_APP_NAME"
    az functionapp create \
      --name "$FUNCTION_APP_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --storage-account "$FUNCTION_STORAGE_ACCOUNT_NAME" \
      --runtime "$FUNCTIONS_RUNTIME" \
      --runtime-version "$FUNCTIONS_RUNTIME_VERSION" \
      --functions-version "$FUNCTIONS_VERSION" \
      --os-type Linux \
      --flexconsumption-location "$PROJECT_LOCATION" \
      --output none
  fi

  log_success "Function App ready: $FUNCTION_APP_NAME"
}

configure_function_settings() {
  log_info "Configuring Function App settings"
  local settings_file
  settings_file="$(mktemp)"
  chmod 600 "$settings_file"

  cat > "$settings_file" <<JSON
{
  "AzureWebJobsStorage": "$FUNCTION_STORAGE_CONNECTION_STRING",
  "ASPNETCORE_ENVIRONMENT": "Production",
  "FUNCTIONS_ENVIRONMENT": "Production",
  "AzureKeyVault__VaultUri": "$KEY_VAULT_URI",
  "AzureKeyVault__CredentialMode": "ManagedIdentity",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "$APPLICATIONINSIGHTS_CONNECTION_STRING"
}
JSON

  az functionapp config appsettings set \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --settings @"$settings_file" \
    --output none

  rm -f "$settings_file"
  log_success "Function App settings configured"
}

ensure_managed_identity_and_keyvault_access() {
  log_info "Assigning managed identity"
  az functionapp identity assign \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --output none

  local principal_id
  principal_id=$(az functionapp identity show \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query principalId \
    -o tsv)

  local kv_id
  kv_id=$(az keyvault show --name "$KEY_VAULT_NAME" --query id -o tsv)

  az role assignment create \
    --assignee "$principal_id" \
    --role "Key Vault Secrets User" \
    --scope "$kv_id" \
    --output none 2>/dev/null || log_warning "Function identity Key Vault role already assigned"

  log_success "Managed identity can read Key Vault secrets"
}

ensure_github_oidc() {
  local repo sp_name app_id app_object_id role_scope subject credential_name
  repo=$(repo_short)
  sp_name="sp-${FUNCTION_APP_NAME}"
  role_scope="/subscriptions/${PROJECT_SUBSCRIPTION_ID}/resourceGroups/${PROJECT_RESOURCE_GROUP}"
  credential_name="GitHubActions-${ENVIRONMENT_PREFIX}-${FUNCTION_APP_NAME}"
  subject="repo:${repo}:ref:refs/heads/${GITHUB_BRANCH}"

  log_info "Ensuring GitHub OIDC service principal: $sp_name"
  app_id=$(az ad app list --display-name "$sp_name" --query "[0].appId" -o tsv 2>/dev/null || true)

  if [[ -z "$app_id" ]]; then
    app_id=$(az ad app create --display-name "$sp_name" --query appId -o tsv)
    az ad sp create --id "$app_id" --output none
  fi

  app_object_id=$(az ad app show --id "$app_id" --query id -o tsv)

  az role assignment create \
    --assignee "$app_id" \
    --role Contributor \
    --scope "$role_scope" \
    --output none 2>/dev/null || log_warning "Contributor role already assigned to OIDC app"

  if ! az ad app federated-credential list \
      --id "$app_object_id" \
      --query "[?name=='${credential_name}'] | [0].name" \
      -o tsv | grep -q "$credential_name"; then
    az ad app federated-credential create \
      --id "$app_object_id" \
      --parameters "{\"name\":\"${credential_name}\",\"issuer\":\"https://token.actions.githubusercontent.com\",\"subject\":\"${subject}\",\"audiences\":[\"api://AzureADTokenExchange\"]}" \
      --output none
  fi

  gh secret set AZURE_CLIENT_ID --body "$app_id" --repo "$repo"
  gh secret set AZURE_TENANT_ID --body "$TENANT_ID" --repo "$repo"
  gh secret set AZURE_SUBSCRIPTION_ID --body "$PROJECT_SUBSCRIPTION_ID" --repo "$repo"
  gh secret set FUNCTION_APP_NAME --body "$FUNCTION_APP_NAME" --repo "$repo"
  gh secret set FUNCTION_BASE_URL --body "https://${FUNCTION_APP_NAME}.azurewebsites.net" --repo "$repo"

  log_success "GitHub OIDC secrets and federated credential ready"
}

print_summary() {
  log_success "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  log_success "  Azure Functions infrastructure ready"
  log_info    "  Function App : $FUNCTION_APP_NAME"
  log_info    "  URL          : https://${FUNCTION_APP_NAME}.azurewebsites.net"
  log_info    "  Health       : https://${FUNCTION_APP_NAME}.azurewebsites.net/api/v1/health"
  log_info    "  Workflow     : .github/workflows/dev-deploy-fintrack-api-functions.yml"
  log_success "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
}

main() {
  load_config
  validate_config
  print_plan

  if [[ "$PLAN_ONLY" == true ]]; then
    exit 0
  fi

  ensure_tools
  setup_azure
  ensure_providers
  ensure_resource_group
  ensure_storage
  ensure_observability_values
  ensure_function_app
  configure_function_settings
  ensure_managed_identity_and_keyvault_access
  ensure_github_oidc
  print_summary
}

main "$@"
