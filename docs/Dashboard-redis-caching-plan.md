# Dashboard Redis Caching Plan

## Purpose

This document defines a focused plan for adding Redis caching to the `dashboard` query so repeated reads do not rebuild the same response from the database when nothing has changed.

---

## Goal

Reduce dashboard latency and database load by caching the final dashboard response per user and filter set, while keeping the data fresh enough for normal product use.

---

## What To Cache

Cache the final `dashboard` response returned by the application service.

Do not start by caching each individual aggregate or repository query separately.

Why:
- simpler implementation
- lower invalidation complexity
- better fit for a read-heavy summary endpoint

---

## Cache Key Strategy

Build the cache key from:
- `userId`
- `fromDate`
- `toDate`
- `activityLimit`

Recommended format:

```text
dashboard:{userId}:{fromDate-or-none}:{toDate-or-none}:{activityLimit}
```

This ensures different filter combinations do not overwrite each other.

---

## Cache Lifetime

Start with a short TTL:
- `60 seconds` recommended default

This gives a strong performance win while keeping the dashboard reasonably fresh even before full invalidation is added.

---

## Invalidation Strategy

Invalidate a user’s dashboard cache when any of the following changes:
- income created, updated, or deleted
- expense created, updated, or deleted
- savings goal created, updated, or deleted
- loan created, updated, or deleted
- loan repayment created, updated, or deleted

Recommended first approach:
- evict all dashboard cache entries for that user

This is simpler and safer than trying to invalidate only one filtered variant.

---

## Implementation Plan

### Phase 1: Add Cache Abstraction

Create an application-level abstraction such as:
- `IDashboardCacheService`

Responsibilities:
- get cached dashboard by key
- set cached dashboard with TTL
- invalidate dashboard cache for a user

This keeps Redis details out of the dashboard service.

### Phase 2: Add Redis Integration

Configure Redis as a distributed cache.

Use it to:
- store serialized dashboard payloads
- share cache across app instances
- support future scaling without changing dashboard logic

### Phase 3: Integrate Into DashboardService

Update `DashboardService` flow:
1. build cache key
2. check Redis first
3. return cached dashboard on hit
4. build dashboard from repository on miss
5. cache the result
6. return the response

### Phase 4: Add Invalidation Hooks

After successful write operations in income, expense, savings, loan, and loan repayment flows:
- trigger dashboard cache eviction for that user

This should happen only after the write succeeds.

### Phase 5: Add Tests

Cover:
- cache miss path
- cache hit path
- different filters produce different keys
- invalidation after mutations
- graceful fallback when Redis is unavailable

---

## Operational Notes

Redis is a strong fit here because:
- dashboard data is read-heavy
- responses are user-specific
- multiple app instances can share one cache
- latency matters more than perfect real-time updates for every request

Redis is not the source of truth. The database remains the source of truth.

If Redis is unavailable:
- the dashboard should still work by falling back to database reads

---

## Rollout Recommendation

1. Introduce the cache abstraction.
2. Add Redis-backed caching with short TTL.
3. Verify cache hit rate and dashboard latency improvement.
4. Add user-level invalidation after mutation flows.
5. Tune TTL only after observing real usage.

---

## Success Criteria

- dashboard responses become noticeably faster on repeat requests
- database load drops for repeated dashboard reads
- stale data is limited to an acceptable short window
- cache failures do not break the dashboard endpoint
