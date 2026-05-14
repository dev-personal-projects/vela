#!/usr/bin/env bash

set -euo pipefail

if [[ ! -S /var/run/docker.sock ]] && [[ -z "${DOCKER_HOST:-}" ]]; then
    echo "[devcontainer] Docker/Testcontainers endpoint not found." >&2
    echo "[devcontainer] Expected /var/run/docker.sock or DOCKER_HOST inside the supported devcontainer." >&2
    exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
    echo "[devcontainer] Docker CLI is not installed in the devcontainer image." >&2
    exit 1
fi

if ! docker version >/dev/null 2>&1; then
    echo "[devcontainer] Docker is present but not reachable from inside the devcontainer." >&2
    echo "[devcontainer] Testcontainers-backed integration tests will not run until Docker access is fixed." >&2
    exit 1
fi

if [[ -f "${HOME}/.docker/config.json" ]] && grep -q '"credsStore"[[:space:]]*:' "${HOME}/.docker/config.json"; then
    cp "${HOME}/.docker/config.json" "${HOME}/.docker/config.json.bak"
    jq 'del(.credsStore)' "${HOME}/.docker/config.json.bak" > "${HOME}/.docker/config.json"
fi

if [[ -z "${TESTCONTAINERS_HOST_OVERRIDE:-}" ]]; then
    export TESTCONTAINERS_HOST_OVERRIDE="host.docker.internal"
fi

echo "[devcontainer] Docker/Testcontainers access is available."
