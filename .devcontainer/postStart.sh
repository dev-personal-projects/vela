#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

bash "${ROOT_DIR}/scripts/devcontainer/validate-env.sh"
bash "${ROOT_DIR}/scripts/devcontainer/start-azurite.sh"
