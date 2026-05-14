#!/usr/bin/env bash

set -uo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
RESULTS_DIR="${ROOT_DIR}/artifacts/test-results/$(date -u +%Y%m%dT%H%M%SZ)"
SUMMARY_SCRIPT="${ROOT_DIR}/scripts/test/summarize-trx.py"

SUITE="all"
if [[ "$#" -gt 0 && "${1}" != -* ]]; then
    SUITE="$1"
    shift
fi

case "${SUITE}" in
    all|unit|integration)
        ;;
    *)
        printf 'Unknown test suite: %s\n' "${SUITE}" >&2
        printf 'Usage: scripts/test/run-tests.sh [all|unit|integration] [dotnet test args...]\n' >&2
        exit 2
        ;;
esac

configure_testcontainers() {
    if [[ "${SUITE}" != "all" && "${SUITE}" != "integration" ]]; then
        return
    fi

    if [[ "${DEVCONTAINER:-}" == "true" && -z "${TESTCONTAINERS_HOST_OVERRIDE:-}" ]]; then
        export TESTCONTAINERS_HOST_OVERRIDE="host.docker.internal"
    fi

    printf '\n'
    printf '==> Testcontainers preflight\n'
    printf '    DEVCONTAINER: %s\n' "${DEVCONTAINER:-<unset>}"
    printf '    DOCKER_HOST: %s\n' "${DOCKER_HOST:-<unset>}"
    printf '    TESTCONTAINERS_HOST_OVERRIDE: %s\n' "${TESTCONTAINERS_HOST_OVERRIDE:-<unset>}"
    printf '    docker.sock: %s\n' "$([[ -S /var/run/docker.sock ]] && printf 'present' || printf 'missing')"

    if command -v docker >/dev/null 2>&1 && docker version >/dev/null 2>&1; then
        printf '    docker: reachable\n'
    else
        printf '    docker: not reachable\n'
    fi
}

run_suite() {
    local suite_name="$1"
    local project_path="$2"
    local trx_name="$3"
    shift 3

    mkdir -p "${RESULTS_DIR}/${suite_name}"

    printf '\n'
    printf '==> Running %s tests\n' "${suite_name}"
    printf '    Project: %s\n' "${project_path}"
    printf '    Results: %s/%s/%s\n' "${RESULTS_DIR}" "${suite_name}" "${trx_name}"
    printf '    Progress: detailed console output is enabled; each test is printed as it completes.\n'
    printf '\n'

    dotnet test "${ROOT_DIR}/${project_path}" \
        --logger "console;verbosity=detailed" \
        --logger "trx;LogFileName=${trx_name}" \
        --results-directory "${RESULTS_DIR}/${suite_name}" \
        "$@"
}

overall_exit_code=0
configure_testcontainers

if [[ "${SUITE}" == "all" || "${SUITE}" == "unit" ]]; then
    run_suite "unit" "Tests/UnitTests/UnitTests.csproj" "unit-tests.trx" "$@"
    suite_exit_code=$?
    if [[ "${suite_exit_code}" -ne 0 ]]; then
        overall_exit_code="${suite_exit_code}"
    fi
fi

if [[ "${SUITE}" == "all" || "${SUITE}" == "integration" ]]; then
    run_suite "integration" "Tests/IntegrationTests/IntegrationTests.csproj" "integration-tests.trx" "$@"
    suite_exit_code=$?
    if [[ "${suite_exit_code}" -ne 0 ]]; then
        overall_exit_code="${suite_exit_code}"
    fi
fi

python3 "${SUMMARY_SCRIPT}" "${RESULTS_DIR}"
summary_exit_code=$?
if [[ "${summary_exit_code}" -ne 0 && "${overall_exit_code}" -eq 0 ]]; then
    overall_exit_code="${summary_exit_code}"
fi

exit "${overall_exit_code}"
