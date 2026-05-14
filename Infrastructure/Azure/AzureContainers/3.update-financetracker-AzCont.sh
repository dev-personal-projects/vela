#!/usr/bin/env bash
#
# =============================================================================
# Finance Tracker GraphQL API - Azure Container Apps update
# =============================================================================
#
# This script updates an existing Finance Tracker Container App without creating
# new infrastructure. It is meant for day-to-day maintenance after the initial
# az containerapp up deployment has created the app and GitHub Actions workflow.
#
# Default behavior:
#   - Verifies Azure authentication and selects the configured subscription.
#   - Ensures the Container App has a system-assigned managed identity.
#   - Ensures that identity can read required runtime secrets from Key Vault.
#   - Ensures the deployment is still pinned to North Europe.
#   - Refreshes deterministic runtime environment variables.
#   - Refreshes Log Analytics and Application Insights settings.
#   - Refreshes CPU, memory, and replica settings from globalenv.config.
#   - Waits for /health unless --no-health is supplied.
#
# Optional behavior:
#   - --image <image> manually pins a container image.
#   - --restart restarts the latest revision.
#   - --trigger-workflow runs the GitHub Actions workflow created by
#     az containerapp up.
#   - --logs prints recent Container App logs after the update.
#
# Usage:
#   ./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh --plan
#   ./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh
#   ./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh --restart --logs
#   ./Infrastructure/Azure/AzureContainers/3.update-financetracker-AzCont.sh --trigger-workflow --workflow <workflow-file.yml>
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
RESTART_APP=false
SHOW_LOGS=false
WAIT_FOR_HEALTH=true
TRIGGER_WORKFLOW=false
WORKFLOW_NAME=""
IMAGE=""
CPU_OVERRIDE=""
MEMORY_OVERRIDE=""
MIN_REPLICAS_OVERRIDE=""
MAX_REPLICAS_OVERRIDE=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --plan) PLAN_ONLY=true; shift ;;
    --yes|-y) AUTO_YES=true; shift ;;
    --restart) RESTART_APP=true; shift ;;
    --logs) SHOW_LOGS=true; shift ;;
    --no-health) WAIT_FOR_HEALTH=false; shift ;;
    --trigger-workflow) TRIGGER_WORKFLOW=true; shift ;;
    --workflow) WORKFLOW_NAME="${2:-}"; shift 2 ;;
    --image) IMAGE="${2:-}"; shift 2 ;;
    --cpu) CPU_OVERRIDE="${2:-}"; shift 2 ;;
    --memory) MEMORY_OVERRIDE="${2:-}"; shift 2 ;;
    --min-replicas) MIN_REPLICAS_OVERRIDE="${2:-}"; shift 2 ;;
    --max-replicas) MAX_REPLICAS_OVERRIDE="${2:-}"; shift 2 ;;
    -h|--help) print_usage; exit 0 ;;
    *) log_error "Unknown option: $1"; print_usage; exit 1 ;;
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
    ENVIRONMENT_PREFIX LOG_FOLDER TENANT_ID PROJECT_SUBSCRIPTION_ID
    PROJECT_RESOURCE_GROUP PROJECT_LOCATION EUROPE_LOCATION EUROPE_PREFIX
    ACR_NAME CONTAINER_APP_NAME CONTAINER_APP_ENV_NAME
    CONTAINER_TARGET_PORT CONTAINER_HEALTH_PATH CONTAINER_CPU CONTAINER_MEMORY
    CONTAINER_MIN_REPLICAS CONTAINER_MAX_REPLICAS KEY_VAULT_NAME KEY_VAULT_URI
    LOG_ANALYTICS_WORKSPACE_NAME APPLICATION_INSIGHTS_NAME
    GITHUB_REPO GITHUB_BRANCH
  )

  for var in "${required[@]}"; do
    if [[ -z "${!var:-}" ]]; then
      log_error "Required variable '$var' is not set in globalenv.config"
      exit 1
    fi
  done
}

repo_short() {
  echo "$GITHUB_REPO" | sed -E 's|^https://github.com/||; s|/$||'
}

ensure_tools() {
  command -v az >/dev/null 2>&1 || { log_error "Azure CLI (az) is required."; exit 1; }

  if [[ "$PLAN_ONLY" != true ]] && ! az extension show --name application-insights >/dev/null 2>&1; then
    log_info "Installing application-insights extension"
    az extension add --name application-insights --only-show-errors
  fi

  if [[ "$TRIGGER_WORKFLOW" == true ]]; then
    command -v gh >/dev/null 2>&1 || { log_error "GitHub CLI (gh) is required for --trigger-workflow."; exit 1; }
    gh auth status --hostname github.com >/dev/null 2>&1 || {
      log_error "GitHub CLI must be authenticated for --trigger-workflow."
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

ensure_north_europe_location() {
  if [[ "$PROJECT_LOCATION" != "northeurope" || "$EUROPE_LOCATION" != "northeurope" || "$EUROPE_PREFIX" != "neu" ]]; then
    log_error "This deployment is pinned to North Europe. Expected PROJECT_LOCATION=northeurope, EUROPE_LOCATION=northeurope, EUROPE_PREFIX=neu."
    exit 1
  fi

  local rg_location
  rg_location=$(az group show --name "$PROJECT_RESOURCE_GROUP" --query location -o tsv)
  if [[ "$rg_location" != "$PROJECT_LOCATION" ]]; then
    log_error "Resource group '$PROJECT_RESOURCE_GROUP' is in '$rg_location', expected '$PROJECT_LOCATION'."
    exit 1
  fi
}

ensure_resources_exist() {
  az group show --name "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1 || {
    log_error "Resource group not found: $PROJECT_RESOURCE_GROUP"
    exit 1
  }

  az containerapp show --name "$CONTAINER_APP_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1 || {
    log_error "Container App not found: $CONTAINER_APP_NAME. Run the deploy script first."
    exit 1
  }

  az keyvault show --name "$KEY_VAULT_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1 || {
    log_error "Key Vault not found: $KEY_VAULT_NAME"
    exit 1
  }

  az monitor log-analytics workspace show --name "$LOG_ANALYTICS_WORKSPACE_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1 || {
    log_error "Log Analytics workspace not found: $LOG_ANALYTICS_WORKSPACE_NAME. Run the deploy script first."
    exit 1
  }

  az monitor app-insights component show --app "$APPLICATION_INSIGHTS_NAME" --resource-group "$PROJECT_RESOURCE_GROUP" >/dev/null 2>&1 || {
    log_error "Application Insights resource not found: $APPLICATION_INSIGHTS_NAME. Run the deploy script first."
    exit 1
  }
}

load_observability_settings() {
  LOG_ANALYTICS_WORKSPACE_ID=$(az monitor log-analytics workspace show \
    --name "$LOG_ANALYTICS_WORKSPACE_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query customerId \
    -o tsv)

  LOG_ANALYTICS_WORKSPACE_KEY=$(az monitor log-analytics workspace get-shared-keys \
    --name "$LOG_ANALYTICS_WORKSPACE_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query primarySharedKey \
    -o tsv)

  APPLICATIONINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show \
    --app "$APPLICATION_INSIGHTS_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query connectionString \
    -o tsv)

  if [[ -z "$APPLICATIONINSIGHTS_CONNECTION_STRING" ]]; then
    log_error "Application Insights connection string was not returned for '$APPLICATION_INSIGHTS_NAME'."
    exit 1
  fi
}

refresh_environment_logging() {
  log_info "Ensuring Container Apps environment logs go to Log Analytics"
  az containerapp env update \
    --name "$CONTAINER_APP_ENV_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --logs-destination log-analytics \
    --logs-workspace-id "$LOG_ANALYTICS_WORKSPACE_ID" \
    --logs-workspace-key "$LOG_ANALYTICS_WORKSPACE_KEY" \
    --output none
}

ensure_keyvault_secrets() {
  local required_secrets=(
    Auth--JwtSecret
    ConnectionStrings--Default
    Supabase--Url
    Supabase--AnonKey
    Redis--ConnectionString
  )
  local missing=()

  for secret_name in "${required_secrets[@]}"; do
    if ! az keyvault secret show \
        --vault-name "$KEY_VAULT_NAME" \
        --name "$secret_name" \
        --query id \
        -o tsv >/dev/null 2>&1; then
      missing+=("$secret_name")
    fi
  done

  if (( ${#missing[@]} > 0 )); then
    log_error "Missing required Key Vault secrets: ${missing[*]}"
    exit 1
  fi

  log_success "Required Key Vault secrets are present"
}

print_plan() {
  local cpu="${CPU_OVERRIDE:-$CONTAINER_CPU}"
  local memory="${MEMORY_OVERRIDE:-$CONTAINER_MEMORY}"
  local min_replicas="${MIN_REPLICAS_OVERRIDE:-$CONTAINER_MIN_REPLICAS}"
  local max_replicas="${MAX_REPLICAS_OVERRIDE:-$CONTAINER_MAX_REPLICAS}"

  echo ""
  echo "============================================================"
  echo "Finance Tracker Azure Container Apps update plan"
  echo "============================================================"
  echo "Subscription        : $PROJECT_SUBSCRIPTION_ID"
  echo "Resource group      : $PROJECT_RESOURCE_GROUP"
  echo "Container App       : $CONTAINER_APP_NAME"
  echo "Key Vault           : $KEY_VAULT_URI"
  echo "Log Analytics       : $LOG_ANALYTICS_WORKSPACE_NAME"
  echo "Application Insights: $APPLICATION_INSIGHTS_NAME"
  echo "Port                : $CONTAINER_TARGET_PORT"
  echo "Health path         : $CONTAINER_HEALTH_PATH"
  echo "CPU / memory        : $cpu / $memory"
  echo "Replicas            : $min_replicas-$max_replicas"
  [[ -n "$IMAGE" ]] && echo "Manual image        : $IMAGE"
  echo "Restart             : $RESTART_APP"
  echo "Wait for health     : $WAIT_FOR_HEALTH"
  echo "Show logs           : $SHOW_LOGS"
  echo "Trigger workflow    : $TRIGGER_WORKFLOW"
  [[ -n "$WORKFLOW_NAME" ]] && echo "Workflow            : $WORKFLOW_NAME"
  echo "============================================================"
  echo ""
}

ensure_keyvault_access() {
  log_info "Ensuring system-assigned managed identity exists"
  az containerapp identity assign \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --system-assigned \
    --output none

  local principal_id
  principal_id=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "identity.principalId" \
    -o tsv)

  local kv_id
  kv_id=$(az keyvault show --name "$KEY_VAULT_NAME" --query id -o tsv)

  log_info "Ensuring managed identity has Key Vault Secrets User"
  az role assignment create \
    --assignee "$principal_id" \
    --role "Key Vault Secrets User" \
    --scope "$kv_id" \
    --output none 2>/dev/null || log_warning "Managed identity Key Vault role already assigned"
}

update_container_app() {
  local cpu="${CPU_OVERRIDE:-$CONTAINER_CPU}"
  local memory="${MEMORY_OVERRIDE:-$CONTAINER_MEMORY}"
  local min_replicas="${MIN_REPLICAS_OVERRIDE:-$CONTAINER_MIN_REPLICAS}"
  local max_replicas="${MAX_REPLICAS_OVERRIDE:-$CONTAINER_MAX_REPLICAS}"

  log_info "Refreshing runtime settings, scale, and resources"
  az containerapp update \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --cpu "$cpu" \
    --memory "$memory" \
    --min-replicas "$min_replicas" \
    --max-replicas "$max_replicas" \
    --set-env-vars \
        ASPNETCORE_ENVIRONMENT=Production \
        ASPNETCORE_URLS="http://0.0.0.0:${CONTAINER_TARGET_PORT}" \
        APPLICATIONINSIGHTS_CONNECTION_STRING="$APPLICATIONINSIGHTS_CONNECTION_STRING" \
        OTEL_SERVICE_NAME="$CONTAINER_APP_NAME" \
        AzureKeyVault__VaultUri="$KEY_VAULT_URI" \
        AzureKeyVault__CredentialMode=ManagedIdentity \
    --output none

  if [[ -n "$IMAGE" ]]; then
    log_info "Updating Container App image: $IMAGE"
    az containerapp update \
      --name "$CONTAINER_APP_NAME" \
      --resource-group "$PROJECT_RESOURCE_GROUP" \
      --image "$IMAGE" \
      --output none
  fi

  log_success "Container App configuration refreshed"
}

restart_container_app() {
  [[ "$RESTART_APP" == true ]] || return 0

  local revision_name
  revision_name=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "properties.latestRevisionName" \
    -o tsv)

  if [[ -z "$revision_name" ]]; then
    log_warning "No latest revision found; skipping restart."
    return 0
  fi

  log_info "Restarting latest Container App revision: $revision_name"
  az containerapp revision restart \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --revision "$revision_name" \
    --output none
  log_success "Container App revision restarted"
}

discover_workflow() {
  if [[ -n "$WORKFLOW_NAME" ]]; then
    echo "$WORKFLOW_NAME"
    return 0
  fi

  local repo
  repo=$(repo_short)
  local workflow
  workflow=$(gh workflow list --repo "$repo" --json name,path \
    --jq ".[] | select((.path | contains(\"container\")) or (.name | contains(\"${CONTAINER_APP_NAME}\")) or (.path | contains(\"${CONTAINER_APP_NAME}\"))) | .path" \
    | head -n 1)

  if [[ -z "$workflow" ]]; then
    log_error "Could not discover the Container Apps workflow. Pass --workflow <workflow-file.yml>."
    exit 1
  fi

  echo "$workflow"
}

trigger_workflow() {
  [[ "$TRIGGER_WORKFLOW" == true ]] || return 0

  local workflow
  workflow=$(discover_workflow)

  log_info "Triggering GitHub Actions workflow '$workflow' on '$GITHUB_BRANCH'"
  gh workflow run "$workflow" \
    --repo "$(repo_short)" \
    --ref "$GITHUB_BRANCH"
  log_success "GitHub Actions workflow queued"
}

wait_for_health() {
  [[ "$WAIT_FOR_HEALTH" == true ]] || return 0

  if ! command -v curl >/dev/null 2>&1; then
    log_warning "curl is not installed; skipping health endpoint wait."
    return 0
  fi

  local fqdn
  fqdn=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --query "properties.configuration.ingress.fqdn" \
    -o tsv)

  local health_url="https://${fqdn}${CONTAINER_HEALTH_PATH}"
  log_info "Waiting for health endpoint: $health_url"

  for attempt in {1..30}; do
    if curl -fsS "$health_url" >/dev/null 2>&1; then
      log_success "Health endpoint is responding"
      return 0
    fi

    log_info "Health check pending (${attempt}/30)"
    sleep 10
  done

  log_warning "Health endpoint did not respond within 5 minutes. Check Container App logs in Azure."
}

show_logs() {
  [[ "$SHOW_LOGS" == true ]] || return 0

  log_info "Showing recent Container App logs"
  az containerapp logs show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$PROJECT_RESOURCE_GROUP" \
    --tail 100
}

main() {
  load_config
  validate_config
  ensure_tools

  mkdir -p "$LOG_FOLDER"
  local log_file="${LOG_FOLDER}/update_$(date +"%Y%m%d_%H%M%S").log"
  exec > >(tee -a "$log_file") 2>&1

  print_plan

  if [[ "$PLAN_ONLY" == true ]]; then
    log_info "Plan mode only. No changes made."
    log_info "Logs saved to: $log_file"
    exit 0
  fi

  setup_azure
  ensure_resources_exist
  ensure_north_europe_location
  load_observability_settings
  refresh_environment_logging
  ensure_keyvault_secrets
  ensure_keyvault_access
  update_container_app
  restart_container_app
  trigger_workflow
  wait_for_health
  show_logs

  log_success "Update complete"
  log_info "Logs saved to: $log_file"
}

main "$@"
