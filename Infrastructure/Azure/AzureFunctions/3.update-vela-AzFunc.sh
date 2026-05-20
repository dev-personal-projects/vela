#!/usr/bin/env bash
set -euo pipefail

declare -r BLUE='\033[0;34m'
declare -r GREEN='\033[0;32m'
declare -r NC='\033[0m'

log_info()    { echo -e "${BLUE}[INFO]    $*${NC}"; }
log_success() { echo -e "${GREEN}[SUCCESS] $*${NC}"; }

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
  echo "globalenv.config not found" >&2
  exit 1
}

main() {
  load_config
  az account set --subscription "$PROJECT_SUBSCRIPTION_ID"

  log_info "Restarting Function App: $FUNCTION_APP_NAME"
  az functionapp restart \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --output none

  log_info "Checking health endpoint"
  curl -fsS "https://${FUNCTION_APP_NAME}.azurewebsites.net/api/v1/health"
  echo
  log_success "Function App update check complete"
}

main "$@"
