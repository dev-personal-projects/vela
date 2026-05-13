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

for command_name in dotnet git gh jq rg psql node npm docker; do
    require_command "${command_name}"
done

if [[ ! -f "${SOLUTION_FILE}" ]]; then
    echo "[devcontainer] Missing solution file: ${SOLUTION_FILE}" >&2
    exit 1
fi

bash "${ROOT_DIR}/scripts/devcontainer/check-testcontainers.sh"

if [[ ! -f "${APPSETTINGS_LOCAL}" ]]; then
    echo "[devcontainer] Missing required local settings file: appsettings.Development.json" >&2
    echo "[devcontainer] This repository expects the real development settings file to be present for the supported dev workflow." >&2
    exit 1
fi

echo "[devcontainer] dotnet SDK: $(dotnet --version)"
echo "[devcontainer] Environment validation passed."
