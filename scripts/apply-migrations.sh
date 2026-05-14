#!/bin/bash

# Apply migrations script
# Applies SQL migration files to the target Supabase database.
# Reads the connection URI from Azure Key Vault by default.
#
# WARNING: This script makes PERMANENT changes to the database.
#     Run validate-migrations.sh first to dry-run before applying.
#
# Usage:
#   ./apply-migrations.sh                                        # reads from Azure Key Vault
#   ./apply-migrations.sh --connection-uri "postgresql://..."    # override URI
#   ./apply-migrations.sh --yes                                  # skip confirmation

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEFAULT_MIGRATIONS_DIR="${SCRIPT_DIR}/db"

CONN_URI=""
MIGRATIONS_DIR=""
AUTO_YES=false

# ── Logging ────────────────────────────────────────────────────────────────────
log_info()  { echo "[INFO]  $*"; }
log_warn()  { echo "[WARN]  $*" >&2; }
log_error() { echo "[ERROR] $*" >&2; }
fatal()     { log_error "$*"; exit 1; }

mask_uri() {
    # Hide password — match everything between first colon-after-user and last @ before host
    echo "$1" | sed -E 's|://([^:]+):[^@]+@|://\1:****@|'
}

# ── Usage ──────────────────────────────────────────────────────────────────────
print_usage() {
    cat <<'USAGE'
Usage: apply-migrations.sh [OPTIONS]

Applies SQL migration files to the Supabase database.
Reads ConnectionStrings--Migration or ConnectionStrings--Default from Azure Key Vault by default.

WARNING: This makes PERMANENT changes. Run validate-migrations.sh first.

Options:
  --connection-uri URI   Explicit one-off connection URI override
                         Format: postgresql://user:pass@host:port/database
  --migrations-dir DIR   Directory containing .sql migration files
                         Default: ./db
  --yes, -y              Skip confirmation prompt
  -h, --help             Show this help message

Examples:
  ./apply-migrations.sh
  ./apply-migrations.sh --yes
  ./apply-migrations.sh --connection-uri "postgresql://postgres:pass@db.<ref>.supabase.co:5432/postgres"
USAGE
}

# ── Arg parsing ────────────────────────────────────────────────────────────────
parse_args() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --connection-uri)
                [[ $# -ge 2 ]] || fatal "--connection-uri requires a value"
                CONN_URI="$2"; shift 2 ;;
            --connection-uri=*)
                CONN_URI="${1#*=}"; shift ;;
            --migrations-dir)
                [[ $# -ge 2 ]] || fatal "--migrations-dir requires a path"
                MIGRATIONS_DIR="$2"; shift 2 ;;
            --migrations-dir=*)
                MIGRATIONS_DIR="${1#*=}"; shift ;;
            --yes|-y)
                AUTO_YES=true; shift ;;
            -h|--help)
                print_usage; exit 0 ;;
            *)
                fatal "Unknown argument: $1. Use --help for usage." ;;
        esac
    done

    MIGRATIONS_DIR="${MIGRATIONS_DIR:-$DEFAULT_MIGRATIONS_DIR}"

    # Read from Azure Key Vault if no override provided.
    if [[ -z "$CONN_URI" ]]; then
        CONN_URI=$(read_conn_uri_from_keyvault)
    fi

    [[ -n "$CONN_URI" ]]       || fatal "No connection URI found in Azure Key Vault or --connection-uri."
    [[ -d "$MIGRATIONS_DIR" ]] || fatal "Migrations directory not found: $MIGRATIONS_DIR"
}

# ── Helpers ────────────────────────────────────────────────────────────────────
read_conn_uri_from_keyvault() {
    local settings_file="${REPO_ROOT}/appsettings.Development.json"
    [[ -f "$settings_file" ]] || settings_file="${REPO_ROOT}/appsettings.json"
    [[ -f "$settings_file" ]] || fatal "No appsettings.json found at $REPO_ROOT"
    command -v jq >/dev/null 2>&1 || fatal "jq is not installed. Install jq or pass --connection-uri."
    command -v az >/dev/null 2>&1 || fatal "az is not installed. Install Azure CLI or pass --connection-uri."

    local vault_uri vault_name uri
    vault_uri="${AzureKeyVault__VaultUri:-$(jq -r '.AzureKeyVault.VaultUri // empty' "$settings_file")}"
    [[ -n "$vault_uri" ]] || fatal "AzureKeyVault:VaultUri is not configured."

    vault_name=$(echo "$vault_uri" | sed -E 's#^https://([^.]+)\..*$#\1#')
    [[ -n "$vault_name" && "$vault_name" != "$vault_uri" ]] || fatal "Could not parse Key Vault name from AzureKeyVault:VaultUri."

    # Prefer Migration connection string (direct, port 5432) over Default (pooler).
    uri=$(az keyvault secret show --vault-name "$vault_name" --name "ConnectionStrings--Migration" --query value -o tsv 2>/dev/null || true)
    if [[ -z "$uri" ]]; then
        uri=$(az keyvault secret show --vault-name "$vault_name" --name "ConnectionStrings--Default" --query value -o tsv 2>/dev/null || true)
    fi

    [[ -n "$uri" ]] || fatal "ConnectionStrings--Migration or ConnectionStrings--Default not found in Key Vault '$vault_name'."
    log_info "Reading connection URI from Azure Key Vault '$vault_name'" >&2
    echo "$uri"
}

get_migration_files() {
    find "$MIGRATIONS_DIR" -maxdepth 1 -name "*.sql" -type f | sort -V
}

prompt_confirm() {
    local answer
    read -rp "$1 [y/N]: " answer
    [[ "${answer,,}" == "y" || "${answer,,}" == "yes" ]]
}

# ── Core ───────────────────────────────────────────────────────────────────────
apply_migrations() {
    local -a migrations
    mapfile -t migrations < <(get_migration_files)

    [[ ${#migrations[@]} -gt 0 ]] || fatal "No .sql files found in $MIGRATIONS_DIR"

    log_info "Found ${#migrations[@]} migration file(s):"
    for m in "${migrations[@]}"; do echo "  - $(basename "$m")"; done
    echo ""
    log_info "Target: $(mask_uri "$CONN_URI")"
    echo ""

    if [[ "$AUTO_YES" != true ]]; then
        log_warn "This will PERMANENTLY apply migrations to the database."
        prompt_confirm "Are you sure you want to proceed?" || { log_info "Cancelled."; exit 0; }
        echo ""
    fi

    local failed=false
    for migration in "${migrations[@]}"; do
        local filename
        filename=$(basename "$migration")
        log_info "Applying: $filename"

        if psql "$CONN_URI" -v ON_ERROR_STOP=1 -f "$migration"; then
            log_info "  ✓ $filename applied"
        else
            log_error "  ✗ $filename failed"
            failed=true
            break
        fi
    done

    echo ""
    if [[ "$failed" == true ]]; then
        log_error "Migration failed. Database may be in a partial state."
        log_error "Review the error above before retrying."
        exit 1
    else
        log_info "All migrations applied successfully!"
    fi
}

# ── Entry ──────────────────────────────────────────────────────────────────────
main() {
    parse_args "$@"

    echo ""
    echo "╔══════════════════════════════════════════════════════╗"
    echo "║              Apply Migrations                        ║"
    echo "║  ⚠️  WARNING: PERMANENT changes to the database      ║"
    echo "╚══════════════════════════════════════════════════════╝"
    echo ""
    echo "  Migrations dir : $MIGRATIONS_DIR"
    echo "  Target         : $(mask_uri "$CONN_URI")"
    echo ""
    echo "  TIP: Run validate-migrations.sh first to dry-run."
    echo ""

    command -v psql >/dev/null 2>&1 || fatal "psql is not installed. Install PostgreSQL client tools."

    apply_migrations
}

main "$@"
