## Summary
- What does this PR change?
- Why is this change needed?

## Problem
- What user, product, or engineering problem does this address?
- Include context that helps a reviewer understand the before/after behavior.

## Scope
- What is intentionally included?
- What is intentionally out of scope?

## Changes
- Describe the main implementation changes.
- Call out important files, modules, or architectural decisions.

## API / Schema Impact
- Does this change any REST, GraphQL, database, or contract behavior?
- If yes, describe:
  - new fields / arguments / payloads
  - changed behavior
  - deprecated or breaking behavior

## Database / Migration Impact
- Are there schema or data migrations?
- If yes, describe:
  - migration files added or changed
  - rollback considerations
  - production / environment impact

## UI / UX Impact
- If user-facing, describe the expected experience change.
- Include screenshots, recordings, or request/response examples when helpful.

## Testing
- What did you test?
- How can a reviewer verify this locally?

### Verification
- [ ] `dotnet build FinanceTracker.csproj`
- [ ] manual API verification completed
- [ ] migrations validated or applied if needed

## Risks
- What could go wrong with this change?
- Call out edge cases, data risks, performance concerns, or security implications.

## Rollout / Ops Notes
- Any deployment sequencing needed?
- Any environment variables, config, or manual steps required?

## Follow-ups
- List any known follow-up work that should happen after this PR.

## Reviewer Checklist
- [ ] Scope is clear and appropriate
- [ ] Business logic is understandable
- [ ] API / schema changes are documented
- [ ] Database changes are safe and validated
- [ ] Error handling is reasonable
- [ ] Tests or verification steps are sufficient
- [ ] Docs / examples were updated where needed
