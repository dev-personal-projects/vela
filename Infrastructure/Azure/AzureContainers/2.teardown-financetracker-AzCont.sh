#!/usr/bin/env bash
#
# =============================================================================
# Finance Tracker GraphQL API - Azure Container Apps teardown
# =============================================================================
#
# This script removes Finance Tracker Azure Container Apps resources created by
# 1.deploy-financetracker-AzCont.sh. It is safe by default:
#
#   - Deletes only the Container App unless additional flags are supplied.
#   - Preserves the Container Apps environment, ACR, Key Vault, resource group,
#     GitHub repository secrets, and deployment service principal by default.
#   - Refuses destructive production actions unless --delete-production is used.
#
# Runtime application secrets remain in Azure Key Vault unless --include-keyvault
# is explicitly supplied.
#
# Usage:
#   ./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --plan
#   ./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh
#   ./Infrastructure/Azure/AzureContainers/2.teardown-financetracker-AzCont.sh --yes
#
# Options:
#   --plan                       Show what would be deleted without changes.
#   --yes, -y                    Non-interactive mode.
#   --include-environment        Delete the Container Apps environment.
#   --include-acr                Delete the Azure Container Registry.
#   --include-keyvault           Delete the Key Vault (soft-delete).
#   --purge-keyvault             Purge the deleted Key Vault too. Requires --include-keyvault.
#   --include-github-automation  Delete GitHub deployment secrets and the deployment service principal.
#   --delete-resource-group      Delete the entire configured resource group.
#   --delete-production          Allow destructive actions for prod/production.
#   -h, --help                   Show help.
#
# =============================================================================
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

trap 'log_error "Failed at line $LINENO: $BASH_COMMAND"; exit 1' ERR

print_usage() {
  awk '
    NR == 1 { next }
    /^set -euo pipefail/ { exit }
    {
      sub(/^# ?/, "")
      if ($0 ~ /^=+$/) { next }
      print
    }
  ' "$0"
}

PLAN_ONLY=false
AUTO_YES=false
INCLUDE_ENVIRONMENT=false
INCLUDE_ACR=false
INCLUDE_KEYVAULT=false
PURGE_KEYVAULT=false
INCLUDE_GITHUB_AUTOMATION=false
DELETE_RESOURCE_GROUP=false
DELETE_PRODUCTION=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --plan) PLAN_ONLY=true; shift ;;
    --yes|-y) AUTO_YES=true; shift ;;
    --include-environment) INCLUDE_ENVIRONMENT=true; shift ;;
    --include-acr) INCLUDE_ACR=true; shift ;;
    --include-keyvault) INCLUDE_KEYVAULT=true; shift ;;
    --purge-keyvault) PURGE_KEYVAULT=true; shift ;;
    --include-github-automation) INCLUDE_GITHUB_AUTOMATION=true; shift ;;
    --delete-resource-group) DELETE_RESOURCE_GROUP=true; shift ;;
    --delete-production) DELETE_PRODUCTION=true; shift ;;
    -h|--help) print_usage; exit 0 ;;
    *) log_error "Unknown option: $1"; print_usage; exit 1 ;;
  esac
done

if [[ "$PURGE_KEYVAULT" == true && "$INCLUDE_KEYVAULT" != true ]]; then
  log_error "--purge-keyvault requires --include-keyvault."
  exit 1
fi

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
    ENVIRONMENT_PREFIX LOG_FOLDER TENANT_ID PROJECT_SUBSCRIPTION_ID
    PROJECT_RESOURCE_GROUP PROJECT_LOCATION ACR_NAME CONTAINER_APP_NAME
    CONTAINER_APP_ENV_NAME KEY_VAULT_NAME GITHUB_REPO
  )

  for var in "${required[@]}"; do
    if [[ -z "${!var:-}" ]]; then
      log_error "Required variable '$var' is not set in globalenv.config"
      exit 1
    fi
  done
}

ensure_tools() {
  command -v az >/dev/null 2>&1 || { log_error "Azure CLI (az) is required."; exit 1; }

  if [[ "$INCLUDE_GITHUB_AUTOMATION" == true ]]; then
    command -v gh >/dev/null 2>&1 || { log_error "GitHub CLI (gh) is required for --include-github-automation."; exit 1; }
    gh auth status --hostname github.com >/dev/null 2>&1 || {
      log_error "GitHub CLI must be authenticated for --include-github-automation."
      exit 1
    }
  fi
}

setup_azure() {
  log_info "Verifying Azure CLI authentication"
  if ! az account show >/dev/null 2>&1; then
    if [[ "$AUTO_YES" == true ]]; then
      log_error "Azure CLI is not authenticated. Run 'az login --tenant $TENANT_ID' and retry."
      exit 1
    fi

    az login --tenant "$TENANT_ID"
  fi

  az account set --subscription "$PROJECT_SUBSCRIPTION_ID"
  local active
  active=$(az account show --query id -o tsv)
  if [[ "$active" != "$PROJECT_SUBSCRIPTION_ID" ]]; then
    log_error "Subscription mismatch. Active: $active, expected: $PROJECT_SUBSCRIPTION_ID"
    exit 1
  fi
}

ensure_production_guard() {
  local env_lower
  env_lower=$(printf '%s' "$ENVIRONMENT_PREFIX" | tr '[:upper:]' '[:lower:]')

  if [[ "$PLAN_ONLY" != true && ( "$env_lower" == "prod" || "$env_lower" == "production" ) && "$DELETE_PRODUCTION" != true ]]; then
    log_error "Refusing destructive teardown in '$ENVIRONMENT_PREFIX' without --delete-production."
    exit 1
  fi
}

repo_short() {
  echo "$GITHUB_REPO" | sed -E 's|^https://github.com/||; s|/$||'
}

print_plan() {
  echo ""
  echo "============================================================"
  echo "Finance Tracker Azure Container Apps teardown plan"
  echo "============================================================"
  echo "Subscription        : $PROJECT_SUBSCRIPTION_ID"
  echo "Resource group      : $PROJECT_RESOURCE_GROUP"
  echo "Container App       : $CONTAINER_APP_NAME"
  echo "Container App Env   : $CONTAINER_APP_ENV_NAME"
  echo "ACR                 : $ACR_NAME"
  echo "Key Vault           : $KEY_VAULT_NAME"
  echo "GitHub repo         : $(repo_short)"
  echo ""

  if [[ "$DELETE_RESOURCE_GROUP" == true ]]; then
    echo "Will delete:"
    echo "  - Resource group and every resource inside it"
  else
    echo "Will delete:"
    echo "  - Container App: $CONTAINER_APP_NAME"
    [[ "$INCLUDE_ENVIRONMENT" == true ]] && echo "  - Container Apps environment: $CONTAINER_APP_ENV_NAME"
    [[ "$INCLUDE_ACR" == true ]] && echo "  - Azure Container Registry: $ACR_NAME"
    [[ "$INCLUDE_KEYVAULT" == true ]] && echo "  - Key Vault: $KEY_VAULT_NAME"
    [[ "$PURGE_KEYVAULT" == true ]] && echo "  - Purged Key Vault tombstone: $KEY_VAULT_NAME"
    [[ "$INCLUDE_GITHUB_AUTOMATION" == true ]] && echo "  - GitHub deployment secrets and service principal"
  fi

  echo ""
  echo "Preserved unless explicitly included:"
  [[ "$DELETE_RESOURCE_GROUP" != true && "$INCLUDE_ENVIRONMENT" != true ]] && echo "  - Container Apps environment"
  [[ "$DELETE_RESOURCE_GROUP" != true && "$INCLUDE_ACR" != true ]] && echo "  - Azure Container Registry"
  [[ "$DELETE_RESOURCE_GROUP" != true && "$INCLUDE_KEYVAULT" != true ]] && echo "  - Key Vault and runtime secrets"
  [[ "$DELETE_RESOURCE_GROUP" != true && "$INCLUDE_GITHUB_AUTOMATION" != true ]] && echo "  - GitHub secrets and deployment service principal"
  echo "============================================================"
  echo ""
}

confirm_teardown() {
  if [[ "$AUTO_YES" == true || "$PLAN_ONLY" == true ]]; then
    return 0
  fi

  local expected="$CONTAINER_APP_NAME"
  if [[ "$DELETE_RESOURCE_GROUP" == true ]]; then
    expected="$PROJECT_RESOURCE_GROUP"
  fi

  log_warning "This is destructive. Type '$expected' to continue:"
  local response
  read -r response
  if [[ "$response" != "$expected" ]]; then
    log_error "Confirmation did not match. Teardown cancelled."
    exit 1
  fi
}

delete_container_app() {
  if az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_info "Deleting Container App: $CONTAINER_APP_NAME"
    az containerapp delete \
      --name "$CONTAINER_APP_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --yes \
      --output none
    log_success "Deleted Container App: $CONTAINER_APP_NAME"
  else
    log_warning "Container App not found: $CONTAINER_APP_NAME"
  fi
}

delete_environment() {
  [[ "$INCLUDE_ENVIRONMENT" == true ]] || return 0

  if az containerapp env show --name "$CONTAINER_APP_ENV_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_info "Deleting Container Apps environment: $CONTAINER_APP_ENV_NAME"
    az containerapp env delete \
      --name "$CONTAINER_APP_ENV_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --yes \
      --output none
    log_success "Deleted Container Apps environment: $CONTAINER_APP_ENV_NAME"
  else
    log_warning "Container Apps environment not found: $CONTAINER_APP_ENV_NAME"
  fi
}

delete_acr() {
  [[ "$INCLUDE_ACR" == true ]] || return 0

  if az acr show --name "$ACR_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_info "Deleting Azure Container Registry: $ACR_NAME"
    az acr delete \
      --name "$ACR_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --yes \
      --output none
    log_success "Deleted Azure Container Registry: $ACR_NAME"
  else
    log_warning "Azure Container Registry not found: $ACR_NAME"
  fi
}

delete_keyvault() {
  [[ "$INCLUDE_KEYVAULT" == true ]] || return 0

  if az keyvault show --name "$KEY_VAULT_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_info "Deleting Key Vault: $KEY_VAULT_NAME"
    az keyvault delete \
      --name "$KEY_VAULT_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --output none
    log_success "Deleted Key Vault: $KEY_VAULT_NAME"
  else
    log_warning "Key Vault not found: $KEY_VAULT_NAME"
  fi

  if [[ "$PURGE_KEYVAULT" == true ]]; then
    log_info "Waiting for Key Vault deletion to finish before purge"
    az keyvault wait \
      --name "$KEY_VAULT_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --deleted \
      --interval 10 \
      --timeout 600

    log_warning "Purging deleted Key Vault: $KEY_VAULT_NAME"
    az keyvault purge \
      --name "$KEY_VAULT_NAME" \
      --location "$PROJECT_LOCATION" \
      --output none
    log_success "Purged Key Vault tombstone: $KEY_VAULT_NAME"
  fi
}

delete_github_automation() {
  [[ "$INCLUDE_GITHUB_AUTOMATION" == true ]] || return 0

  local repo
  repo=$(repo_short)
  local secrets=(
    AZURE_CREDENTIALS
    AZURE_CLIENT_ID
    AZURE_CLIENT_SECRET
    AZURE_TENANT_ID
    AZURE_SUBSCRIPTION_ID
    AZURE_RESOURCE_GROUP
    ACR_NAME
    CONTAINER_APP_NAME
    CONTAINER_APP_ENV_NAME
    KEYVAULT_NAME
    KEYVAULT_URI
  )

  for secret_name in "${secrets[@]}"; do
    log_info "Deleting GitHub secret if present: $secret_name"
    gh secret delete "$secret_name" --repo "$repo" >/dev/null 2>&1 || log_warning "GitHub secret not found or could not be deleted: $secret_name"
  done

  local sp_name="sp-${CONTAINER_APP_NAME}"
  local app_ids
  app_ids=$(az ad sp list --display-name "$sp_name" --query "[].appId" -o tsv 2>/dev/null || true)

  if [[ -z "$app_ids" ]]; then
    log_warning "Deployment service principal not found: $sp_name"
    return 0
  fi

  local app_id
  while IFS= read -r app_id; do
    [[ -n "$app_id" ]] || continue
    log_info "Deleting deployment service principal: $sp_name ($app_id)"
    az ad sp delete --id "$app_id" --output none
  done <<< "$app_ids"

  log_success "GitHub automation cleanup complete"
}

delete_resource_group() {
  [[ "$DELETE_RESOURCE_GROUP" == true ]] || return 1

  if az group show --name "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_warning "Deleting entire resource group: $PROJECT_RESOURCE_GROUP"
    az group delete \
      --name "$PROJECT_RESOURCE_GROUP" \
      --yes \
      --no-wait \
      --output none
    log_success "Resource group deletion started: $PROJECT_RESOURCE_GROUP"
  else
    log_warning "Resource group not found: $PROJECT_RESOURCE_GROUP"
  fi

  return 0
}

main() {
  load_config
  validate_config
  ensure_tools
  ensure_production_guard

  mkdir -p "$LOG_FOLDER"
  local log_file="${LOG_FOLDER}/teardown_$(date +"%Y%m%d_%H%M%S").log"
  exec > >(tee -a "$log_file") 2>&1

  print_plan

  if [[ "$PLAN_ONLY" == true ]]; then
    log_info "Plan mode only. No changes made."
    log_info "Logs saved to: $log_file"
    exit 0
  fi

  setup_azure
  confirm_teardown

  if delete_resource_group; then
    delete_github_automation
    log_info "Logs saved to: $log_file"
    exit 0
  fi

  delete_container_app
  delete_environment
  delete_acr
  delete_keyvault
  delete_github_automation

  log_success "Teardown complete"
  log_info "Logs saved to: $log_file"
}

main "$@"
