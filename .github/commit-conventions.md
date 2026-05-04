# Commit Conventions

## Purpose

This document defines the required commit workflow for this repository.

The goal is to keep commits:
- branch-aware
- reviewable
- descriptive of the full change scope
- consistent across human and agent-driven work

The source of truth for this workflow is:
- this document
- `.github/commit-message-template.md`
- `scripts/git/prepare-commit-context.sh`
- `scripts/git/finalize-commit.sh`

## Core Rules

Every commit in this repository should follow these rules:

1. The commit message must describe the actual scope of the staged changes being committed.
2. The commit message must be generated only after reviewing branch context against the default branch.
3. The proposed commit message must be written to a file in the repository root before the commit is created.
4. The commit message may use an emoji when it adds clarity, but emoji use is optional and should stay restrained.
5. The message must stay short, readable, and outcome-focused.
6. Unrelated work must not be mixed into the same commit.
7. The agent must never stage files on the user's behalf.
8. If no changes are staged, the agent must ask the user to stage the intended files before proceeding.

## Required Pre-Commit Review

Before creating a commit, always review:
- the current staged and unstaged changes
- the staged diff specifically
- the current branch name
- the recent `HEAD` history
- the commits on the feature branch that differ from the default branch
- the diff summary against the default branch

The agent is responsible for gathering that context before generating the proposed commit message.

Recommended review command:

```bash
bash scripts/git/prepare-commit-context.sh
```

This script discovers the repository default branch through `gh` and uses staged scope plus branch context to generate the proposal file:

```text
commit-message-proposal.md
```

The file should contain only the final commit message paragraph.
The proposal file is a generated artifact, not a long-form review document.
After the developer confirms the proposal and instructs the agent to proceed, the agent should use that exact paragraph as the commit message.
After the commit is created and the current branch is pushed successfully, the generated proposal file should be removed.

## Commit Message Standard

The final commit message should be a short paragraph that:
- describes the staged scope clearly
- includes the important parts of the change
- avoids shallow labels that hide the real work
- may include one emoji at the beginning when it improves readability

The goal is not to produce a rigid subject template. The goal is to produce a readable commit message that captures the real staged work.

Prefer messages like:

```text
📝 This normalizes the repository documentation structure by standardizing filenames, updating references, and aligning the documentation tree with the new naming convention.

🧪 This strengthens the repository testing workflow by clarifying the staged-only commit process, generating a reviewable proposal file, and adding scripts to prepare and finalize approved commits.
```

Avoid messages like:

```text
chore: updates
🚀 chore: define branch-aware commit workflow
fix: stuff
```

## Emoji Guidance

Emoji usage is allowed when it improves scanability.

Recommended examples:
- `📝` for documentation
- `🧪` for tests
- `🚀` for developer workflow or tooling improvements
- `🐛` for bug fixes
- `♻️` for refactors

Rules for emoji usage:
- use at most one emoji
- place it at the start of the subject
- do not use emoji if it makes the subject noisy or unclear
- prefer clarity over decoration

## Message Rules

The final paragraph should:
- describe the outcome, not just the activity
- remain concise while still covering the important staged scope
- match the full scope of the current commit
- read naturally if someone scans history later

## Staged-Only Commit Rule

Commits in this repository are created from staged changes only.

That means:
- the commit message must describe the staged scope, not the whole working tree
- unstaged work may inform branch context review, but must not be silently included in the commit
- the agent must never run `git add` or otherwise stage files without the user's explicit instruction
- if nothing is staged, the agent must stop and ask the user to stage the intended files first

## Branch-Aware Commit Rule

The commit message should reflect everything changed in the staged commit relative to the feature branch scope and the default branch context.

That means:
- inspect what the branch is introducing
- inspect what has already been committed on the branch
- avoid writing a message that describes only the last edited file
- avoid a message that is narrower than the staged scope

If the staged changes are broader than one coherent unit:
- split the commit
- or restage a smaller scope

## Agent Workflow

When an agent is preparing a commit, it must:

1. Run `bash scripts/git/prepare-commit-context.sh`.
2. Stop if the script reports that nothing is staged.
3. Use branch context, recent history, and the staged diff to generate a proposed commit message.
4. Write or update `commit-message-proposal.md` in the repository root.
5. Ask the developer to review and confirm the proposal file.
6. When the developer instructs the agent to proceed, read the confirmed final commit message directly from `commit-message-proposal.md`.
7. Create the commit from staged changes only.
8. Push the current branch.
9. Remove the generated proposal file after a successful push.

The agent should not invent a commit message from one file or one headline without looking at the branch context first.
The agent should not stage files automatically.

## Developer Workflow

The expected developer flow is:

1. Stage the intended files manually.
2. Run `bash scripts/git/prepare-commit-context.sh`, or ask the agent to do it.
3. Review `commit-message-proposal.md`.
4. If the proposal is correct, tell the agent to proceed.
5. The agent runs `bash scripts/git/finalize-commit.sh`.
6. The agent commits the staged changes, pushes the current branch, and removes the generated proposal file.

The developer does not need to stage through the agent.
The developer does not need to rewrite the proposal file manually when the generated message is already correct.

## Proposal File

Before committing, write `commit-message-proposal.md` as plain text containing only the final commit message paragraph.

It should not include:
- branch metadata
- staged diff summaries
- markdown sections
- review checklists
- placeholder templates

It exists only to hold the final approved commit message that `scripts/git/finalize-commit.sh` will use.

## Recommended `gh`-Assisted Flow

Use GitHub CLI to understand repository context:

```bash
gh repo view --json nameWithOwner,defaultBranchRef
```

Use Git to inspect the branch and stage intentionally:

```bash
bash scripts/git/prepare-commit-context.sh
```

If nothing is staged, stop and stage the intended files manually first.

Then propose the commit message, review it, and commit:

```bash
bash scripts/git/finalize-commit.sh
```

## Commit Message Template Reference

The standard reusable template lives in:

- [.github/commit-message-template.md](/workspaces/finance-tracker-graphql/.github/commit-message-template.md)
- [scripts/git/prepare-commit-context.sh](/workspaces/finance-tracker-graphql/scripts/git/prepare-commit-context.sh)
- [scripts/git/finalize-commit.sh](/workspaces/finance-tracker-graphql/scripts/git/finalize-commit.sh)
- [commit-message-proposal.md](/workspaces/finance-tracker-graphql/commit-message-proposal.md)
