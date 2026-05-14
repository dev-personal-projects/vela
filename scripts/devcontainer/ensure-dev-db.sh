#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "${ROOT_DIR}"

echo "[devcontainer] Validating development database migrations..."
bash "${ROOT_DIR}/scripts/validate-migrations.sh" --yes

echo "[devcontainer] Applying development database migrations..."
bash "${ROOT_DIR}/scripts/apply-migrations.sh" --yes

echo "[devcontainer] Development database schema is up to date."
