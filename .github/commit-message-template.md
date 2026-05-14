# Commit Message Template

Use this format for every commit in this repository:

```text
<emoji> <type>: This <describes the staged change clearly in one short sentence with the important scope and impact>.
```

The emoji and type are required. The message must be one paragraph, outcome-focused, and specific to the staged files.

## Required Types

Use one type that best describes the staged change:

| Type | Icon | Use for |
| --- | --- | --- |
| `feat` | ✨ | user-facing features or new behavior |
| `fix` | 🐛 | normal bug fixes or behavioral corrections |
| `repair` | 🛠️ | urgent repair work, production recovery, or corrective cleanup after a failed change |
| `security` | 🔐 | authentication, authorization, secrets, cryptography, or sensitive-data hardening |
| `test` | 🧪 | test coverage, test fixtures, or test-only infrastructure |
| `docs` | 📝 | documentation-only changes |
| `ci` | 🤖 | GitHub Actions, checks, release automation, or repository CI |
| `build` | 🏗️ | build system, project files, package restore, or dependency wiring |
| `refactor` | ♻️ | internal restructuring without behavior change |
| `perf` | ⚡ | performance improvements |
| `style` | 🎨 | formatting or style-only code changes |
| `chore` | 🔧 | repository maintenance, workflow housekeeping, or non-product tooling |
| `release` | 🚀 | versioning, release notes, or release preparation |

## Rules

- Start with exactly one matching icon.
- Follow the icon with exactly one type and a colon.
- Start the sentence after the colon with `This`.
- Keep the message to one short paragraph.
- Describe the outcome, not only the activity.
- Match the full staged scope.
- Do not mix unrelated changes into one commit.

## Good Examples

```text
🔐 security: This migrates runtime secret management to Azure Key Vault and removes local secret placeholders from the application configuration.
🤖 ci: This adds GitHub Actions validation that restores, builds, and runs unit and integration tests before branch merges.
📝 docs: This clarifies the commit message standard by requiring an icon, a typed prefix, and an outcome-focused summary for every commit.
🛠️ repair: This corrects the broken CI restore path by targeting tracked project files instead of the ignored local solution file.
```

## Poor Examples

```text
fix stuff
docs: update docs
📝 update template
🔧 chore: changes
```

## Agent Workflow

1. Review staged files and branch context.
2. Write the final proposed message to `commit-message-proposal.md`.
3. Ensure the proposal follows this template exactly.
4. Commit only the staged files with the approved proposal.
5. Remove `commit-message-proposal.md` after the commit is pushed.
