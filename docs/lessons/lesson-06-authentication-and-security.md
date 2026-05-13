# Lesson 6 — Authentication and Security in GraphQL

## How Auth Works in This Project

We use a hybrid approach — REST for auth, GraphQL for everything else.

```mermaid
flowchart TB
    subgraph Step 1 - Get a token via REST
        SIGNUP["POST /api/auth/signup\n{ email, password }\n→ { message }"]
        SIGNIN["POST /api/auth/signin\n{ email, password }\n→ { accessToken, refreshToken, tokenType }"]
    end

    subgraph Step 2 - Use token with GraphQL
        GQL["POST /graphql\nAuthorization: Bearer accessToken\n→ data"]
    end

    SIGNIN --> GQL
```

---

## JWT — JSON Web Token

A JWT is a self-contained token that carries claims about the user. It has three parts separated by dots:

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9    ← Header (Base64)
.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ikpv  ← Payload (Base64)
.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw  ← Signature (HMAC)
```

### Header
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

### Payload (claims)
```json
{
  "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "iat": 1704067200,
  "exp": 1704070800
}
```

- `sub` — subject — the user's ID — this is what we extract as `UserId`
- `iat` — issued at — Unix timestamp
- `exp` — expiry — Unix timestamp
- `email` — custom claim we add

### Signature
The signature is created by hashing the header + payload with our secret key. If anyone tampers with the token, the signature won't match and validation fails.

---

## JWT Validation Flow

```mermaid
sequenceDiagram
    participant Client
    participant JWTMiddleware
    participant GraphQL

    Client->>JWTMiddleware: POST /graphql\nAuthorization: Bearer eyJhbGci...

    JWTMiddleware->>JWTMiddleware: 1. Decode header\nget algorithm
    JWTMiddleware->>JWTMiddleware: 2. Verify signature\nusing Auth:JwtSecret
    JWTMiddleware->>JWTMiddleware: 3. Check expiry\nexp > now
    JWTMiddleware->>JWTMiddleware: 4. Extract claims\nsub = UserId

    alt Token invalid or expired
        JWTMiddleware-->>Client: 401 Unauthorized
    else Token valid
        JWTMiddleware->>GraphQL: forward + ClaimsPrincipal
        GraphQL-->>Client: data
    end
```

---

## Current Token Model In This Project

```mermaid
flowchart TB
    S1[User signs in via REST]
    S2[API verifies credentials through Supabase]
    S3[API issues its own JWT access token]
    S4[Client sends Bearer token to /graphql]

    S1 --> S2 --> S3 --> S4
```

Today this backend is built around a single API access token for authorization:

- The API issues its own JWT after successful sign-in.
- The token lifetime is controlled by `Auth:AccessTokenMinutes` and defaults to 60 minutes.
- The client sends that token in the `Authorization: Bearer <token>` header for GraphQL requests.
- The sign-in response also includes the upstream refresh token returned by Supabase.
- There is no `/api/auth/refresh` endpoint implemented in this codebase right now.

### What should a client do when the token expires?

```kotlin
// Apollo interceptor — attach token when available
class AuthInterceptor : HttpInterceptor {
    override suspend fun intercept(request: HttpRequest, chain: HttpInterceptorChain): HttpResponse {
        val token = tokenStore.getAccessToken()

        val authenticatedRequest = request.newBuilder().apply {
            if (!token.isNullOrBlank()) {
                addHeader("Authorization", "Bearer $token")
            }
        }.build()

        val response = chain.proceed(authenticatedRequest)

        if (response.statusCode == 401) {
            tokenStore.clear()
            // Redirect user to sign in again or trigger your app's re-auth flow.
        }

        return response
    }
}
```

If you add refresh-token support later, that should be documented as a new auth flow, not assumed by default. Returning the refresh token in the sign-in response does not by itself mean the API already supports token refresh.

---

## UserId Scoping — Data Isolation

Every piece of data in this project is scoped to the authenticated user. This is enforced at three levels:

```mermaid
flowchart TB
    subgraph Level 1 - Resolver
        R["claimsPrincipal.GetUserId()\nextract from JWT"]
    end

    subgraph Level 2 - Service
        S["service.GetAllAsync(userId)\npass userId to every operation"]
    end

    subgraph Level 3 - Repository
        DB["WHERE user_id = @userId\nfilter at database level"]
    end

    R --> S --> DB
```

Even if someone passes a valid JWT for User A, they can only ever see User A's data. The `userId` extracted from the token is used in every database query.

```csharp
// Repository — userId is always in the WHERE clause
public Task<List<Income>> GetAllAsync(Guid userId, CancellationToken ct) =>
    db.Incomes
      .Where(x => x.UserId == userId)   // ← enforced here
      .OrderByDescending(x => x.Date)
      .ToListAsync(ct);
```

---

## The `[Authorize]` Attribute in Hot Chocolate

```csharp
public sealed class Query
{
    // Public — no token needed
    public string Health() => "Finance Tracker API is running!";

    // Protected — valid JWT required
    [Authorize]
    public async Task<List<Income>> Incomes(
        [Service] IIncomeService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken ct)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, ct);
        return result.Value!;
    }
}
```

When `[Authorize]` is present and no valid JWT is provided:

```json
{
  "errors": [
    {
      "message": "The current user is not authorized to access this resource.",
      "extensions": { "code": "AUTH_NOT_AUTHENTICATED" }
    }
  ],
  "data": null
}
```

---

## Our Custom JWT — Why We Moved Away From Supabase JWT

Originally we tried to validate Supabase's JWT tokens directly. We hit two problems:

1. Supabase tokens include a `kid` (Key ID) header that the validator uses for key lookup
2. The JWT secret encoding (Base64 vs UTF-8) was ambiguous

**The solution** — we generate our own JWT tokens. Supabase is used only to verify the email/password. Once verified, we issue our own token signed with `Auth:JwtSecret`.

```mermaid
flowchart LR
    subgraph Old approach - problematic
        O1[User signs in]
        O2[Supabase returns Supabase JWT]
        O3[Our API validates Supabase JWT]
        O4[kid mismatch issues]
        O1 --> O2 --> O3 --> O4
    end

    subgraph New approach - clean
        N1[User signs in]
        N2[Supabase verifies password]
        N3[Our API issues our own JWT]
        N4[Our API validates our own JWT]
        N1 --> N2 --> N3 --> N4
    end
```

Benefits:
- Full control over token format and claims
- No dependency on Supabase JWT format
- Works identically locally and on Azure
- Easy to add custom claims (roles, permissions) later

---

## Security Best Practices Applied

| Practice | How we apply it |
|---|---|
| Short-lived access tokens | 60 minutes — configurable via `Auth:AccessTokenMinutes` |
| JWT signature validation | Every request validated with `Auth:JwtSecret` |
| UserId from token only | Never trust UserId from request body |
| Data scoping | Every DB query filters by `UserId` |
| Secrets in config | Never hardcoded — `appsettings.Development.json` is gitignored |
| HTTPS in production | Azure provides free SSL — all traffic encrypted |
| Password hashing | Supabase handles this — bcrypt internally |

---

## What Happens When the Token Expires

```mermaid
sequenceDiagram
    participant AndroidApp
    participant API

    AndroidApp->>API: POST /graphql\nBearer expired_token
    API-->>AndroidApp: 401 Unauthorized

    AndroidApp->>AndroidApp: Clear token / mark session expired
    AndroidApp->>API: POST /api/auth/signin\nemail + password
    API-->>AndroidApp: { accessToken, tokenType }
    AndroidApp->>API: POST /graphql\nBearer new_access_token
    API-->>AndroidApp: data
```

The current backend contract is simple: expired token means the request is rejected, and the client must obtain a fresh token by signing in again. That keeps the auth story easy to understand while the API is still focused on the core finance flows.

---

## When To Use These Security Concepts

- Use REST sign-in first whenever you need a token for GraphQL, because `/graphql` is for domain operations, not credential exchange.
- Use `[Authorize]` on resolvers that read or mutate user-owned financial data such as `incomes`, `expenses`, `loans`, and `dashboard`.
- Use `claimsPrincipal.GetUserId()` whenever the authenticated user should drive the query, which prevents cross-user data access.
- Use the public `health` query when you want to check whether the GraphQL endpoint is reachable before debugging auth problems.
- Use short-lived access tokens when you want a simpler, safer MVP auth model before introducing refresh-token complexity.

---

## Key Takeaways

| Concept | What it means |
|---|---|
| JWT | Self-contained token with user claims — no DB lookup needed |
| `sub` claim | The user's ID — extracted as `UserId` in every resolver |
| Access token | Short-lived — used for every API request |
| `[Authorize]` | Rejects requests without a valid JWT |
| `GetUserId()` | Extracts `sub` claim from `ClaimsPrincipal` |
| Data scoping | Every query filters by `UserId` — cross-user access impossible |
| Our own JWT | We issue tokens — Supabase only verifies passwords |
| `Auth:JwtSecret` | Our signing key — never committed to git |
