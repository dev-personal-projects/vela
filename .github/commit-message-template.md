# Commit Message Template

Use this template after reviewing:
- `bash scripts/git/prepare-commit-context.sh`

Only use this template when the intended commit already has staged changes.
The proposal should be written into:

```text
commit-message-proposal.md
```

The file should contain only the final commit message paragraph.
After the developer confirms it and tells the agent to proceed, the agent should use that exact paragraph as the commit message.
The developer does not need to rewrite the proposal manually.
After a successful commit and push, the generated proposal file should be removed.

```text
<optional-emoji> This <describe the staged change clearly in one short paragraph including the important scope and impact>.
```

## Examples

```text
📝 This normalizes the repository documentation and naming conventions by renaming the docs tree, updating links, and keeping the main references aligned across the project.
🧪 This aligns the integration testing workflow with the staged-only commit process by adding proposal generation, finalize scripts, and shared commit guidance for reviewed branch work.
🚀 This updates the repository workflow tooling by adding helper scripts, editor tasks, and a reviewed commit flow that lets the agent commit and push approved staged changes safely.
```
