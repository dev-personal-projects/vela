#!/usr/bin/env bash
#
# =============================================================================
# Finance Tracker GraphQL API - Azure Container Apps deployment
# =============================================================================
#
# This script provisions and deploys the Finance Tracker backend API to Azure
# Container Apps. The application is a .NET GraphQL/REST API that loads runtime
# secrets from Azure Key Vault during startup.
#
# What this script does:
#   1. Loads non-secret deployment identifiers from Infrastructure/Azure/globalenv.config.
#   2. Verifies Azure CLI authentication and selects the configured subscription.
#   3. Creates the resource group, Azure Container Registry, and Container Apps environment if needed.
#   4. Creates a GitHub Actions service principal and stores deployment automation
#      credentials as GitHub repository secrets.
#   5. Deploys the API through `az containerapp up`.
#   6. Enables a system-assigned managed identity on the Container App.
#   7. Grants that managed identity Key Vault Secrets User access so the API can
#      read the required runtime secrets at startup.
#
# Runtime secrets are NOT stored in this file or passed as plain app settings.
# They must already exist in the configured Key Vault:
#   - Auth--JwtSecret
#   - ConnectionStrings--Default
#   - Supabase--Url
#   - Supabase--AnonKey
#   - Redis--ConnectionString
#
# Usage:
#   ./Infrastructure/Azure/AzureContainers/1.deploy-financetracker-AzCont.sh
#
# Prerequisites:
#   - Azure CLI authenticated with `az login`
#   - GitHub CLI authenticated with `gh auth login`
#   - Required Key Vault secrets already present
#
# =============================================================================
set -euo pipefail

# ── Colors ────────────────────────────────────────────────────────────────────
declare -r RED='\033[0;31m'
declare -r GREEN='\033[0;32m'
declare -r YELLOW='\033[0;33m'
declare -r BLUE='\033[0;34m'
declare -r NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]    $*${NC}"; }
log_success() { echo -e "${GREEN}[SUCCESS] $*${NC}"; }
log_warning() { echo -e "${YELLOW}[WARNING] $*${NC}"; }
log_error()   { echo -e "${RED}[ERROR]   $*${NC}" >&2; }

trap 'log_error "Failed at line $LINENO: $BASH_COMMAND"; exit 1' ERR

# ── Load config ───────────────────────────────────────────────────────────────
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

# ── Validate required vars ────────────────────────────────────────────────────
validate_config() {
  local required=(
    ENVIRONMENT_PREFIX PROJECT_PREFIX PROJECT_LOCATION
    PROJECT_RESOURCE_GROUP PROJECT_SUBSCRIPTION_ID TENANT_ID
    ACR_NAME CONTAINER_APP_NAME CONTAINER_APP_ENV_NAME
    KEY_VAULT_NAME KEY_VAULT_URI IMAGE_NAME IMAGE_TAG
    GITHUB_REPO GITHUB_BRANCH LOG_FOLDER
  )
  for var in "${required[@]}"; do
    if [[ -z "${!var:-}" ]]; then
      log_error "Required variable '$var' is not set in globalenv.config"
      exit 1
    fi
  done
}

# ── Azure auth ────────────────────────────────────────────────────────────────
setup_azure() {
  command -v az >/dev/null 2>&1 || { log_error "Azure CLI (az) is required."; exit 1; }

  log_info "Verifying Azure CLI authentication"
  if ! az account show &>/dev/null; then
    log_warning "Not logged in — initiating az login"
    az login --tenant "$TENANT_ID"
  fi

  az account set --subscription "$PROJECT_SUBSCRIPTION_ID"
  local active
  active=$(az account show --query id -o tsv)
  if [[ "$active" != "$PROJECT_SUBSCRIPTION_ID" ]]; then
    log_error "Subscription mismatch. Active: $active, Expected: $PROJECT_SUBSCRIPTION_ID"
    exit 1
  fi
  log_success "Azure subscription: $PROJECT_SUBSCRIPTION_ID"
}

# ── Prerequisites ─────────────────────────────────────────────────────────────
ensure_prereqs() {
  command -v gh >/dev/null 2>&1 || { log_error "GitHub CLI (gh) is required."; exit 1; }

  if ! az extension show --name containerapp &>/dev/null; then
    log_info "Installing containerapp extension"
    az extension add --name containerapp --only-show-errors
  fi
  az provider register --namespace Microsoft.App --wait 2>/dev/null || true
  az provider register --namespace Microsoft.OperationalInsights --wait 2>/dev/null || true
  az provider register --namespace Microsoft.ContainerRegistry --wait 2>/dev/null || true
  log_success "Prerequisites ready"
}

# ── Resource group ────────────────────────────────────────────────────────────
ensure_resource_group() {
  if ! az group show --name "$PROJECT_RESOURCE_GROUP" &>/dev/null; then
    log_warning "Resource group '$PROJECT_RESOURCE_GROUP' not found — creating"
    az group create \
      --name "$PROJECT_RESOURCE_GROUP" \
      --location "$PROJECT_LOCATION" \
      --output none
  fi
  log_success "Resource group ready: $PROJECT_RESOURCE_GROUP"
}

# ── ACR ───────────────────────────────────────────────────────────────────────
ensure_acr() {
  if ! az acr show --name "$ACR_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" &>/dev/null; then
    log_info "Creating Azure Container Registry: $ACR_NAME"
    az acr create \
      --name "$ACR_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --location "$PROJECT_LOCATION" \
      --sku Basic \
      --admin-enabled true \
      --output none
  fi
  log_success "ACR ready: ${ACR_NAME}.azurecr.io"
}

# ── Container Apps environment ────────────────────────────────────────────────
ensure_container_app_env() {
  if ! az containerapp env show \
      --name "$CONTAINER_APP_ENV_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" &>/dev/null; then
    log_info "Creating Container Apps environment: $CONTAINER_APP_ENV_NAME"
    az containerapp env create \
      --name "$CONTAINER_APP_ENV_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --location "$PROJECT_LOCATION" \
      --output none
  fi
  log_success "Container Apps environment ready: $CONTAINER_APP_ENV_NAME"
}

# ── Service principal + GitHub secrets ──────────────────────────────────────
ensure_service_principal_and_github_secrets() {
  local sp_name="sp-${CONTAINER_APP_NAME}"
  local repo_short
  repo_short=$(echo "$GITHUB_REPO" | sed 's|https://github.com/||')

  log_info "Creating or resetting deployment service principal: $sp_name"

  local subscription_id
  subscription_id=$(az account show --query id -o tsv)

  # Create or reset the service principal scoped to the resource group
  local sp_json
  sp_json=$(az ad sp create-for-rbac \
    --name "$sp_name" \
    --role Contributor \
    --scopes "/subscriptions/${subscription_id}/resourceGroups/${PROJECT_RESOURCE_GROUP}" \
    --sdk-auth \
    --output json)

  log_success "Deployment service principal ready: $sp_name"

  # Extract individual values for granular secrets
  SP_CLIENT_ID=$(echo "$sp_json"     | grep -o '"clientId": *"[^"]*"'     | grep -o '"[^"]*"$' | tr -d '"')
  SP_CLIENT_SECRET=$(echo "$sp_json" | grep -o '"clientSecret": *"[^"]*"' | grep -o '"[^"]*"$' | tr -d '"')
  SP_TENANT_ID=$(echo "$sp_json"     | grep -o '"tenantId": *"[^"]*"'     | grep -o '"[^"]*"$' | tr -d '"')

  log_info "Saving secrets to GitHub repo: $repo_short"

  # Full SDK auth JSON — used by azure/login action
  gh secret set AZURE_CREDENTIALS        --body "$sp_json"               --repo "$repo_short"
  # Granular secrets — used by individual Azure actions
  gh secret set AZURE_CLIENT_ID          --body "$SP_CLIENT_ID"          --repo "$repo_short"
  gh secret set AZURE_CLIENT_SECRET      --body "$SP_CLIENT_SECRET"      --repo "$repo_short"
  gh secret set AZURE_TENANT_ID          --body "$SP_TENANT_ID"          --repo "$repo_short"
  gh secret set AZURE_SUBSCRIPTION_ID    --body "$subscription_id"       --repo "$repo_short"
  gh secret set AZURE_RESOURCE_GROUP     --body "$PROJECT_RESOURCE_GROUP" --repo "$repo_short"
  gh secret set ACR_NAME                 --body "$ACR_NAME"              --repo "$repo_short"
  gh secret set CONTAINER_APP_NAME       --body "$CONTAINER_APP_NAME"    --repo "$repo_short"
  gh secret set CONTAINER_APP_ENV_NAME   --body "$CONTAINER_APP_ENV_NAME" --repo "$repo_short"
  gh secret set KEYVAULT_NAME            --body "$KEY_VAULT_NAME"        --repo "$repo_short"
  gh secret set KEYVAULT_URI             --body "$KEY_VAULT_URI"         --repo "$repo_short"

  log_success "All secrets saved to GitHub repo: $repo_short"

  # Grant the deployment SP Key Vault Secrets User so GitHub Actions can validate
  # Key Vault access without receiving the actual application secret values.
  local kv_id
  kv_id=$(az keyvault show --name "$KEY_VAULT_NAME" --query id -o tsv)
  az role assignment create \
    --assignee "$SP_CLIENT_ID" \
    --role "Key Vault Secrets User" \
    --scope "$kv_id" \
    --output none 2>/dev/null || log_warning "SP Key Vault role already assigned — skipping"
}

# ── az containerapp up ────────────────────────────────────────────────────────
# This command creates or updates the Container App and wires GitHub deployment
# automation for the configured branch. The app receives only bootstrap
# configuration here. Runtime secrets remain in Azure Key Vault and are loaded
# by Program.cs through AddFinanceTrackerKeyVaultAsync.
deploy_via_containerapp_up() {
  log_info "Running az containerapp up"
  log_info "  Repo  : $GITHUB_REPO"
  log_info "  Branch: $GITHUB_BRANCH"
  log_info "  Image : ${ACR_NAME}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}"

  az containerapp up \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --environment "$CONTAINER_APP_ENV_NAME" \
    --repo "$GITHUB_REPO" \
    --branch "$GITHUB_BRANCH" \
    --registry-server "${ACR_NAME}.azurecr.io" \
    --service-principal-client-id "$SP_CLIENT_ID" \
    --service-principal-client-secret "$SP_CLIENT_SECRET" \
    --service-principal-tenant-id "$SP_TENANT_ID" \
    --ingress external \
    --target-port "${CONTAINER_TARGET_PORT:-8080}" \
    --env-vars \
        ASPNETCORE_ENVIRONMENT=Production \
        ASPNETCORE_URLS="http://0.0.0.0:${CONTAINER_TARGET_PORT:-8080}" \
        AzureKeyVault__VaultUri="$KEY_VAULT_URI" \
        AzureKeyVault__CredentialMode=ManagedIdentity

  log_success "Container App deployed: $CONTAINER_APP_NAME"
}

# ── Scale & resource config ───────────────────────────────────────────────────
configure_scaling() {
  log_info "Configuring scaling and resources"
  az containerapp update \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --cpu 0.5 \
    --memory 1.0Gi \
    --min-replicas 1 \
    --max-replicas 5 \
    --output none
  log_success "Scaling configured: 1–5 replicas, 0.5 CPU, 1Gi memory"
}

# ── Managed identity + Key Vault access ───────────────────────────────────────
ensure_keyvault_access() {
  log_info "Assigning system-managed identity"
  az containerapp identity assign \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --system-assigned \
    --output none

  local principal_id
  principal_id=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "identity.principalId" -o tsv)

  log_info "Assigning Key Vault Secrets User RBAC role to managed identity"
  local kv_id
  kv_id=$(az keyvault show --name "$KEY_VAULT_NAME" --query id -o tsv)

  az role assignment create \
    --assignee "$principal_id" \
    --role "Key Vault Secrets User" \
    --scope "$kv_id" \
    --output none 2>/dev/null || log_warning "Managed identity Key Vault role already assigned — skipping"

  log_success "Managed identity can read runtime secrets from Key Vault"
}

# ── Print summary ─────────────────────────────────────────────────────────────
print_summary() {
  local fqdn
  fqdn=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "properties.configuration.ingress.fqdn" -o tsv)

  log_success "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  log_success "  Deployment complete!"
  log_info   "  App URL     : https://${fqdn}"
  log_info   "  Health check: https://${fqdn}/health"
  log_info   "  GitHub Actions workflow committed to: $GITHUB_REPO"
  log_info   "  Future pushes to '$GITHUB_BRANCH' will auto-deploy"
  log_success "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
}

# ── Main ──────────────────────────────────────────────────────────────────────
main() {
  load_config
  validate_config

  local timestamp
  timestamp=$(date +"%Y%m%d_%H%M%S")
  local log_file="${LOG_FOLDER}/deploy_${timestamp}.log"
  mkdir -p "$LOG_FOLDER"
  exec > >(tee -a "$log_file") 2>&1

  log_info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  log_info "  Finance Tracker GraphQL API — Azure Container Apps Deployment"
  log_info "  Environment : $ENVIRONMENT_PREFIX"
  log_info "  Resource Grp: $PROJECT_RESOURCE_GROUP"
  log_info "  Location    : $PROJECT_LOCATION"
  log_info "  Key Vault   : $KEY_VAULT_URI"
  log_info "  Repo        : $GITHUB_REPO ($GITHUB_BRANCH)"
  log_info "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

  setup_azure
  ensure_prereqs
  ensure_resource_group
  ensure_acr
  ensure_container_app_env
  ensure_service_principal_and_github_secrets
  deploy_via_containerapp_up
  configure_scaling
  ensure_keyvault_access
  print_summary

  log_info "Logs saved to: $log_file"
}

main "$@"
