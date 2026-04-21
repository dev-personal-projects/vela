#!/bin/bash

# Dry-run migration validation script
# Validates that migration scripts will apply cleanly WITHOUT committing changes.
# Reads the connection URI from appsettings.json by default.
#
# How it works:
#   1. Reads connection URI from appsettings.json (or --connection-uri override)
#   2. Starts a transaction
#   3. Runs all migration scripts in order
#   4. Rolls back the transaction — no changes persisted
#   5. Reports success or failure
#
# Usage:
#   ./validate-migrations.sh                                      # reads from appsettings.json
#   ./validate-migrations.sh --connection-uri "postgresql://..."  # override URI
#   ./validate-migrations.sh --verbose

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEFAULT_MIGRATIONS_DIR="${SCRIPT_DIR}/db"

CONN_URI=""
MIGRATIONS_DIR=""
VERBOSE=false
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
Usage: validate-migrations.sh [OPTIONS]

Validates migration scripts by running them inside a transaction that is rolled back.
Reads ConnectionStrings.Default from appsettings.json by default.
No changes are persisted to the database.

Options:
  --connection-uri URI   Override the connection URI from appsettings.json
                         Format: postgresql://user:pass@host:port/database
  --migrations-dir DIR   Directory containing .sql migration files
                         Default: ./db
  --verbose              Show full SQL output during validation
  --yes, -y              Skip confirmation prompt
  -h, --help             Show this help message

Examples:
  ./validate-migrations.sh
  ./validate-migrations.sh --verbose
  ./validate-migrations.sh --connection-uri "postgresql://postgres:pass@db.<ref>.supabase.co:5432/postgres"
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
            --verbose)
                VERBOSE=true; shift ;;
            --yes|-y)
                AUTO_YES=true; shift ;;
            -h|--help)
                print_usage; exit 0 ;;
            *)
                fatal "Unknown argument: $1. Use --help for usage." ;;
        esac
    done

    MIGRATIONS_DIR="${MIGRATIONS_DIR:-$DEFAULT_MIGRATIONS_DIR}"

    # Read from appsettings.json if no override provided
    if [[ -z "$CONN_URI" ]]; then
        CONN_URI=$(read_conn_uri_from_appsettings)
    fi

    [[ -n "$CONN_URI" ]]       || fatal "No connection URI found. Set ConnectionStrings.Default in appsettings.json or pass --connection-uri."
    [[ -d "$MIGRATIONS_DIR" ]] || fatal "Migrations directory not found: $MIGRATIONS_DIR"
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

get_migration_files() {
    find "$MIGRATIONS_DIR" -maxdepth 1 -name "*.sql" -type f | sort -V
}

prompt_confirm() {
    local answer
    read -rp "$1 [y/N]: " answer
    [[ "${answer,,}" == "y" || "${answer,,}" == "yes" ]]
}

build_combined_script() {
    local migrations=("$@")

    echo "BEGIN;"
    echo ""
    echo "-- Dry-run validation — this transaction will be rolled back"
    echo ""

    for migration in "${migrations[@]}"; do
        local filename
        filename=$(basename "$migration")
        echo "-- ────────────────────────────────────────────────────"
        echo "-- Applying: $filename"
        echo "-- ────────────────────────────────────────────────────"
        echo ""
        printf "\\echo '>>> Applying: %s'\n" "$filename"
        echo ""
        printf "\\i %s\n" "$migration"
        echo ""
    done

    echo "-- ────────────────────────────────────────────────────"
    printf "\\echo '>>> All migrations validated — rolling back (dry-run)'\n"
    echo "ROLLBACK;"
}

# ── Core ───────────────────────────────────────────────────────────────────────
validate_migrations() {
    local -a migrations
    mapfile -t migrations < <(get_migration_files)

    [[ ${#migrations[@]} -gt 0 ]] || fatal "No .sql files found in $MIGRATIONS_DIR"

    log_info "Found ${#migrations[@]} migration file(s) to validate:"
    for m in "${migrations[@]}"; do echo "  - $(basename "$m")"; done
    echo ""
    log_info "Target: $(mask_uri "$CONN_URI")"
    echo ""

    if [[ "$AUTO_YES" != true ]]; then
        prompt_confirm "Proceed with dry-run validation?" || { log_info "Validation cancelled."; exit 0; }
        echo ""
    fi

    local temp_script
    temp_script=$(mktemp /tmp/migration-validate-XXXXXX.sql)
    trap 'rm -f "$temp_script"' EXIT

    build_combined_script "${migrations[@]}" > "$temp_script"

    if [[ "$VERBOSE" == true ]]; then
        log_info "Combined validation script:"
        echo "---"
        cat "$temp_script"
        echo "---"
        echo ""
    fi

    local psql_opts=("-v" "ON_ERROR_STOP=1" "-f" "$temp_script")
    [[ "$VERBOSE" != true ]] && psql_opts+=("--quiet")

    if psql "$CONN_URI" "${psql_opts[@]}"; then
        echo ""
        log_info "✓ All migrations validated successfully!"
        log_info "  No changes were applied (transaction was rolled back)."
    else
        echo ""
        log_error "✗ Migration validation failed!"
        log_info "  No changes were applied. Review the error above."
        exit 1
    fi
}

# ── Entry ──────────────────────────────────────────────────────────────────────
main() {
    parse_args "$@"

    echo ""
    echo "╔══════════════════════════════════════════════════════╗"
    echo "║           Migration Dry-Run Validation               ║"
    echo "║   No changes will be applied to the database.        ║"
    echo "╚══════════════════════════════════════════════════════╝"
    echo ""
    echo "  Migrations dir : $MIGRATIONS_DIR"
    echo "  Target         : $(mask_uri "$CONN_URI")"
    echo ""

    command -v psql >/dev/null 2>&1 || fatal "psql is not installed. Install PostgreSQL client tools."

    validate_migrations
}

main "$@"
