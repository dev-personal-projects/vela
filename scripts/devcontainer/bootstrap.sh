#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SOLUTION_FILE="${ROOT_DIR}/finance-tracker-graphql.sln"

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
bash "${ROOT_DIR}/scripts/devcontainer/ensure-dev-db.sh"

echo "[bootstrap] Devcontainer bootstrap complete."
