#!/usr/bin/env bash

set -euo pipefail

PROPOSAL_FILE="commit-message-proposal.md"
LEGACY_PROPOSAL_FILE="COMMIT_MESSAGE_PROPOSAL.md"

if ! command -v git >/dev/null 2>&1; then
    echo "git is required." >&2
    exit 1
fi

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "Run this script inside a git repository." >&2
    exit 1
fi

if [[ ! -f "${PROPOSAL_FILE}" ]]; then
    echo "Missing ${PROPOSAL_FILE}. Run the commit context helper first." >&2
    exit 1
fi

if [[ -z "$(git diff --cached --stat)" ]]; then
    echo "No staged changes found. Stage the intended files first." >&2
    exit 2
fi

COMMIT_MESSAGE="$(sed -e '/^[[:space:]]*$/d' "${PROPOSAL_FILE}")"

if [[ -z "${COMMIT_MESSAGE}" ]]; then
    echo "Could not read the final commit message from ${PROPOSAL_FILE}." >&2
    exit 1
fi

CURRENT_BRANCH="$(git branch --show-current)"

printf 'Using commit message\n'
printf '%s\n\n' "${COMMIT_MESSAGE}"

git commit -m "${COMMIT_MESSAGE}"
git push -u origin "${CURRENT_BRANCH}"

rm -f "${PROPOSAL_FILE}" "${LEGACY_PROPOSAL_FILE}"

printf 'Cleanup\n'
printf 'Removed generated proposal files.\n'
