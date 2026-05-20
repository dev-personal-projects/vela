#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SOLUTION_FILE="${ROOT_DIR}/build/vela.sln"
APPSETTINGS_LOCAL="${ROOT_DIR}/appsettings.Development.json"

require_command() {
    local command_name="$1"

    if ! command -v "${command_name}" >/dev/null 2>&1; then
        echo "[devcontainer] Missing required command: ${command_name}" >&2
        exit 1
    fi
}

warn_or_exit() {
    local message="$1"

    if [[ "${DEVCONTAINER_STRICT_VALIDATION:-0}" == "1" ]]; then
        echo "${message}" >&2
        exit 1
    fi

    echo "${message}" >&2
}

cd "${ROOT_DIR}"

for command_name in dotnet git jq rg psql node npm; do
    require_command "${command_name}"
done

for command_name in gh docker az; do
    if ! command -v "${command_name}" >/dev/null 2>&1; then
        warn_or_exit "[devcontainer] Optional command is unavailable: ${command_name}"
    fi
done

if [[ ! -f "${SOLUTION_FILE}" ]]; then
    echo "[devcontainer] Missing solution file: ${SOLUTION_FILE}" >&2
    exit 1
fi

if command -v docker >/dev/null 2>&1; then
    if ! bash "${ROOT_DIR}/scripts/devcontainer/check-testcontainers.sh"; then
        warn_or_exit "[devcontainer] Docker/Testcontainers validation failed. Integration tests may not run until Docker access is fixed."
    fi
fi

if command -v az >/dev/null 2>&1 && ! az account show >/dev/null 2>&1; then
    warn_or_exit "[devcontainer] Azure CLI is installed but not logged in. Run: az login"
fi

vault_uri="${AzureKeyVault__VaultUri:-}"
if [[ -z "${vault_uri}" && -f "${APPSETTINGS_LOCAL}" ]]; then
    vault_uri="$(jq -r '.AzureKeyVault.VaultUri // empty' "${APPSETTINGS_LOCAL}")"
fi

if [[ -z "${vault_uri}" ]]; then
    warn_or_exit "[devcontainer] Missing Azure Key Vault bootstrap config. Set AzureKeyVault__VaultUri or create appsettings.Development.json from appsettings.Development.example.json."
fi

echo "[devcontainer] dotnet SDK: $(dotnet --version)"
echo "[devcontainer] Environment validation passed."
