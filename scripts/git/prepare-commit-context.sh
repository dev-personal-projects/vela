#!/usr/bin/env bash

set -euo pipefail

PROPOSAL_FILE="commit-message-proposal.md"

if ! command -v git >/dev/null 2>&1; then
    echo "git is required." >&2
    exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
    echo "gh is required." >&2
    exit 1
fi

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "Run this script inside a git repository." >&2
    exit 1
fi

DEFAULT_BRANCH="$(gh repo view --json defaultBranchRef --jq '.defaultBranchRef.name')"
STAGED_DIFF="$(git diff --cached --stat)"
STAGED_FILES="$(git diff --cached --name-only || true)"

generate_commit_message() {
    if printf '%s\n' "${STAGED_FILES}" | rg -q '(^|/)(commit-conventions\.md|commit-message-template\.md|prepare-commit-context\.sh)$'; then
        printf '%s\n' '🚀 This updates the repository commit workflow by adding branch-aware commit conventions, a reusable commit message proposal file, VS Code tasks, and helper scripts to prepare and finalize reviewed commits from staged changes only.'
        return
    fi

    if printf '%s\n' "${STAGED_FILES}" | rg -q '(^|/)docs/|(^|/)README\.md$'; then
        printf '%s\n' '📝 This updates the repository documentation to keep naming, guidance, and developer workflow references consistent across the docs set and the main README.'
        return
    fi

    if printf '%s\n' "${STAGED_FILES}" | rg -q '(^|/)Tests/'; then
        printf '%s\n' '🧪 This expands automated test coverage and strengthens the shared testing foundation so staged changes are validated more consistently across the current feature scope.'
        return
    fi

    if printf '%s\n' "${STAGED_FILES}" | rg -q '(^|/)\.github/|(^|/)\.vscode/|(^|/)scripts/'; then
        printf '%s\n' '🚀 This updates repository workflow tooling by improving the shared automation, editor tasks, and supporting scripts used to prepare and complete branch work safely.'
        return
    fi

    printf '%s\n' 'This updates the staged branch work with the reviewed changes currently prepared for commit and keeps the scope aligned with the confirmed feature context.'
}

DRAFT_COMMIT_MESSAGE="$(generate_commit_message)"

if [[ -z "${STAGED_DIFF}" ]]; then
    printf 'No staged changes found.\n'
    printf 'Stage the intended files first, then rerun this script.\n'
    exit 2
fi

printf '%s\n' "${DRAFT_COMMIT_MESSAGE}" > "${PROPOSAL_FILE}"

printf 'Generated %s with the final commit message.\n' "${PROPOSAL_FILE}"
printf 'If the message looks right, tell the agent to proceed with the commit.\n'
