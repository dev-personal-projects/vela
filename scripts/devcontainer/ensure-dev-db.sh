#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "${ROOT_DIR}"

if [[ ! -f "${ROOT_DIR}/appsettings.Development.json" ]]; then
    echo "[devcontainer] Missing appsettings.Development.json; cannot ensure the development database schema." >&2
    exit 1
fi

echo "[devcontainer] Validating development database migrations..."
bash "${ROOT_DIR}/scripts/validate-migrations.sh" --yes

echo "[devcontainer] Applying development database migrations..."
bash "${ROOT_DIR}/scripts/apply-migrations.sh" --yes

echo "[devcontainer] Development database schema is up to date."
