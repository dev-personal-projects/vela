#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SOLUTION_FILE="${ROOT_DIR}/build/vela.sln"

cd "${ROOT_DIR}"

echo "[bootstrap] Restoring solution dependencies..."
dotnet restore "${SOLUTION_FILE}"

if [[ -f "${ROOT_DIR}/.config/dotnet-tools.json" ]]; then
    echo "[bootstrap] Restoring local dotnet tools..."
    dotnet tool restore
fi

echo "[bootstrap] Validating devcontainer environment..."
bash "${ROOT_DIR}/scripts/devcontainer/validate-env.sh"

echo "[bootstrap] Ensuring development database schema..."
if ! bash "${ROOT_DIR}/scripts/devcontainer/ensure-dev-db.sh"; then
    if [[ "${DEVCONTAINER_STRICT_VALIDATION:-0}" == "1" ]]; then
        exit 1
    fi

    echo "[bootstrap] Development database schema was not applied. Run scripts/devcontainer/ensure-dev-db.sh after Azure/database config is ready." >&2
fi

echo "[bootstrap] Devcontainer bootstrap complete."
