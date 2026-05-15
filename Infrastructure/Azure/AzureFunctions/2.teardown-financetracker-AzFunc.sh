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

AUTO_YES=false
INCLUDE_STORAGE=false
INCLUDE_GITHUB_AUTOMATION=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --yes|-y) AUTO_YES=true; shift ;;
    --include-storage) INCLUDE_STORAGE=true; shift ;;
    --include-github-automation) INCLUDE_GITHUB_AUTOMATION=true; shift ;;
    -h|--help)
      echo "Usage: $0 [--yes] [--include-storage] [--include-github-automation]"
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
      return 0
    fi
    dir="$(dirname "$dir")"
  done
  log_error "globalenv.config not found"
  exit 1
}

repo_short() {
  echo "$GITHUB_REPO" | sed -E 's|^https://github.com/||; s|/$||'
}

confirm() {
  [[ "$AUTO_YES" == true ]] && return 0
  log_warning "Type '$FUNCTION_APP_NAME' to delete the Function App:"
  local response
  read -r response
  [[ "$response" == "$FUNCTION_APP_NAME" ]] || { log_error "Confirmation mismatch."; exit 1; }
}

main() {
  load_config
  az account set --subscription "$PROJECT_SUBSCRIPTION_ID"
  confirm

  if az functionapp show --name "$FUNCTION_APP_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
    log_info "Deleting Function App: $FUNCTION_APP_NAME"
    az functionapp delete \
      --name "$FUNCTION_APP_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --output none
    log_success "Deleted Function App: $FUNCTION_APP_NAME"
  else
    log_warning "Function App not found: $FUNCTION_APP_NAME"
  fi

  if [[ "$INCLUDE_STORAGE" == true ]]; then
    if az storage account show --name "$FUNCTION_STORAGE_ACCOUNT_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1; then
      log_info "Deleting Function storage account: $FUNCTION_STORAGE_ACCOUNT_NAME"
      az storage account delete \
        --name "$FUNCTION_STORAGE_ACCOUNT_NAME" \
        --resource-group "$PROJECT_RESOURCE_GROUP" \
        --yes \
        --output none
      log_success "Deleted Function storage account: $FUNCTION_STORAGE_ACCOUNT_NAME"
    fi
  fi

  if [[ "$INCLUDE_GITHUB_AUTOMATION" == true ]]; then
    local repo
    repo=$(repo_short)
    for secret_name in AZURE_CLIENT_ID AZURE_TENANT_ID AZURE_SUBSCRIPTION_ID FUNCTION_APP_NAME FUNCTION_BASE_URL; do
      gh secret delete "$secret_name" --repo "$repo" >/dev/null 2>&1 || true
    done
  fi

  log_success "Azure Functions teardown complete"
}

main "$@"
