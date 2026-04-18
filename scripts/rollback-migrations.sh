#!/bin/bash

# Rollback migrations script
# Rolls back migrations by running the rollback SQL files in scripts/db/rollback/.
# Reads the connection URI from appsettings.json by default.
#
# DANGER: This PERMANENTLY DELETES tables and data. Cannot be undone.
#
# Usage:
#   ./rollback-migrations.sh                                      # reads from appsettings.json
#   ./rollback-migrations.sh --connection-uri "postgresql://..."  # override URI
#   ./rollback-migrations.sh --yes

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
ROLLBACK_DIR="${SCRIPT_DIR}/db/rollback"

CONN_URI=""
AUTO_YES=false

# ── Logging ────────────────────────────────────────────────────────────────────
log_info()  { echo "[INFO]  $*"; }
log_warn()  { echo "[WARN]  $*" >&2; }
log_error() { echo "[ERROR] $*" >&2; }
fatal()     { log_error "$*"; exit 1; }

mask_uri() {
    echo "$1" | sed -E 's|://([^:]+):[^@]+@|://\1:****@|'
}

# ── Usage ──────────────────────────────────────────────────────────────────────
print_usage() {
    cat <<'USAGE'
Usage: rollback-migrations.sh [OPTIONS]

Rolls back migrations by executing rollback SQL files in scripts/db/rollback/.
Reads ConnectionStrings.Default from appsettings.json by default.

DANGER: This PERMANENTLY DELETES tables and data. Cannot be undone.

Options:
  --connection-uri URI   Override the connection URI from appsettings.json
                         Format: postgresql://user:pass@host:port/database
  --yes, -y              Skip confirmation prompts (DANGEROUS)
  -h, --help             Show this help message

Examples:
  ./rollback-migrations.sh
  ./rollback-migrations.sh --connection-uri "postgresql://postgres:pass@db.<ref>.supabase.co:5432/postgres"
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
            --yes|-y)
                AUTO_YES=true; shift ;;
            -h|--help)
                print_usage; exit 0 ;;
            *)
                fatal "Unknown argument: $1. Use --help for usage." ;;
        esac
    done

    [[ -n "$CONN_URI" ]]        || CONN_URI=$(read_conn_uri_from_appsettings)
    [[ -n "$CONN_URI" ]]        || fatal "No connection URI found. Set ConnectionStrings.Default in appsettings.json or pass --connection-uri."
    [[ -d "$ROLLBACK_DIR" ]]    || fatal "Rollback directory not found: $ROLLBACK_DIR"
}

# ── Helpers ────────────────────────────────────────────────────────────────────
read_conn_uri_from_appsettings() {
    local settings_file="${REPO_ROOT}/appsettings.Development.json"
    [[ -f "$settings_file" ]] || settings_file="${REPO_ROOT}/appsettings.json"
    [[ -f "$settings_file" ]] || fatal "No appsettings.json found at $REPO_ROOT"
    command -v jq >/dev/null 2>&1 || fatal "jq is not installed. Install jq or pass --connection-uri."
    local uri
    uri=$(jq -r '.ConnectionStrings.Migration // .ConnectionStrings.Default // empty' "$settings_file")
    [[ -n "$uri" ]] || fatal "ConnectionStrings.Migration or .Default not found in $settings_file"
    log_info "Reading connection URI from $(basename "$settings_file")" >&2
    echo "$uri"
}

get_rollback_files() {
    # Rollback order: drop dependent tables first, then parent tables
    # Explicit preferred order — reverse of migration apply order
    local -a preferred=(
        "$ROLLBACK_DIR/rollback_loans.sql"
        "$ROLLBACK_DIR/rollback_savings_goals.sql"
        "$ROLLBACK_DIR/rollback_expenses.sql"
        "$ROLLBACK_DIR/rollback_incomes.sql"
        "$ROLLBACK_DIR/rollback_profiles.sql"
    )

    declare -A seen
    for f in "${preferred[@]}"; do
        if [[ -f "$f" ]]; then
            seen["$f"]=1
            printf '%s\n' "$f"
        fi
    done

    # Append any additional rollback scripts not in the preferred list
    while IFS= read -r f; do
        if [[ -z "${seen[$f]:-}" ]]; then
            printf '%s\n' "$f"
        fi
    done < <(find "$ROLLBACK_DIR" -maxdepth 1 -name "*.sql" -type f | sort -rV)
}

prompt_confirm() {
    local answer
    read -rp "$1 [y/N]: " answer
    [[ "${answer,,}" == "y" || "${answer,,}" == "yes" ]]
}

# ── Core ───────────────────────────────────────────────────────────────────────
execute_rollback() {
    local -a rollback_files
    mapfile -t rollback_files < <(get_rollback_files)

    [[ ${#rollback_files[@]} -gt 0 ]] || fatal "No rollback .sql files found in $ROLLBACK_DIR"

    log_info "Found ${#rollback_files[@]} rollback file(s):"
    for f in "${rollback_files[@]}"; do echo "  - $(basename "$f")"; done
    echo ""
    log_info "Target: $(mask_uri "$CONN_URI")"
    echo ""

    if [[ "$AUTO_YES" != true ]]; then
        log_warn "⚠️  DANGER: This will PERMANENTLY DELETE tables and data!"
        log_warn "⚠️  This action CANNOT be undone!"
        echo ""
        prompt_confirm "Are you ABSOLUTELY SURE you want to rollback?" || { log_info "Rollback cancelled."; exit 0; }
        echo ""
        prompt_confirm "Final confirmation — type yes to confirm PERMANENT DELETION" || { log_info "Rollback cancelled."; exit 0; }
        echo ""
    fi

    local failed=false
    for rollback_file in "${rollback_files[@]}"; do
        local filename
        filename=$(basename "$rollback_file")
        log_info "Executing: $filename"

        if psql "$CONN_URI" -v ON_ERROR_STOP=1 -f "$rollback_file"; then
            log_info "  ✓ $filename executed"
        else
            log_error "  ✗ $filename failed"
            failed=true
            break
        fi
    done

    echo ""
    if [[ "$failed" == true ]]; then
        log_error "Rollback failed. Database may be in a partial state."
        log_error "Review the error above and fix manually."
        exit 1
    else
        log_info "Rollback completed successfully. All tables removed."
    fi
}

# ── Entry ──────────────────────────────────────────────────────────────────────
main() {
    parse_args "$@"

    echo ""
    echo "╔══════════════════════════════════════════════════════╗"
    echo "║        ⚠️  ROLLBACK MIGRATIONS — DANGER ZONE ⚠️       ║"
    echo "║   This will PERMANENTLY DELETE tables and data!      ║"
    echo "╚══════════════════════════════════════════════════════╝"
    echo ""
    echo "  Rollback dir : $ROLLBACK_DIR"
    echo "  Target       : $(mask_uri "$CONN_URI")"
    echo ""

    command -v psql >/dev/null 2>&1 || fatal "psql is not installed. Install PostgreSQL client tools."

    execute_rollback
}

main "$@"
