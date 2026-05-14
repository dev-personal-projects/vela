#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SOLUTION_FILE="${ROOT_DIR}/finance-tracker-graphql.sln"
APPSETTINGS_LOCAL="${ROOT_DIR}/appsettings.Development.json"

require_command() {
    local command_name="$1"

    if ! command -v "${command_name}" >/dev/null 2>&1; then
        echo "[devcontainer] Missing required command: ${command_name}" >&2
        exit 1
    fi
}

cd "${ROOT_DIR}"

for command_name in dotnet git gh jq rg psql node npm docker az; do
    require_command "${command_name}"
done

if [[ ! -f "${SOLUTION_FILE}" ]]; then
    echo "[devcontainer] Missing solution file: ${SOLUTION_FILE}" >&2
    exit 1
fi

bash "${ROOT_DIR}/scripts/devcontainer/check-testcontainers.sh"

if ! az account show >/dev/null 2>&1; then
    echo "[devcontainer] Azure CLI is installed but not logged in. Run: az login" >&2
    exit 1
fi

vault_uri="${AzureKeyVault__VaultUri:-}"
if [[ -z "${vault_uri}" && -f "${APPSETTINGS_LOCAL}" ]]; then
    vault_uri="$(jq -r '.AzureKeyVault.VaultUri // empty' "${APPSETTINGS_LOCAL}")"
fi

if [[ -z "${vault_uri}" ]]; then
    echo "[devcontainer] Missing Azure Key Vault bootstrap config." >&2
    echo "[devcontainer] Set AzureKeyVault__VaultUri or create appsettings.Development.json from appsettings.Development.example.json." >&2
    exit 1
fi

echo "[devcontainer] dotnet SDK: $(dotnet --version)"
echo "[devcontainer] Environment validation passed."
