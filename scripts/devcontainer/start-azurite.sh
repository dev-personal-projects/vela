#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
LOG_DIR="${ROOT_DIR}/logs/azurite"

if ! command -v azurite >/dev/null 2>&1; then
    echo "[azurite] Azurite is not installed; AzureWebJobsStorage=UseDevelopmentStorage=true will be unavailable." >&2
    exit 0
fi

if ss -ltn | grep -q ':10000\b' &&
   ss -ltn | grep -q ':10001\b' &&
   ss -ltn | grep -q ':10002\b'; then
    echo "[azurite] Storage emulator ports are already in use; assuming Azurite is running."
    exit 0
fi

mkdir -p "${LOG_DIR}"

setsid azurite \
    --silent \
    --location "${LOG_DIR}" \
    --debug "${LOG_DIR}/debug.log" \
    --blobHost 0.0.0.0 \
    --queueHost 0.0.0.0 \
    --tableHost 0.0.0.0 \
    > "${LOG_DIR}/azurite.log" 2>&1 < /dev/null &

sleep 1

if ss -ltn | grep -q ':10000\b' &&
   ss -ltn | grep -q ':10001\b' &&
   ss -ltn | grep -q ':10002\b'; then
    echo "[azurite] Started local Azure Storage emulator."
else
    echo "[azurite] Failed to start. See ${LOG_DIR}/azurite.log." >&2
    exit 1
fi
