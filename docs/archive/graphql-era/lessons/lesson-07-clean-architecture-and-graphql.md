# Lesson 7 — Clean Architecture and How It Connects to GraphQL

## Why Architecture Matters

Without a clear architecture, a GraphQL API becomes a mess fast — resolvers talk directly to the database, business logic leaks into queries, and testing becomes impossible.

Clean Architecture solves this by enforcing strict rules about which layer can talk to which.

---

## The Four Layers

```mermaid
flowchart TB
    subgraph API Layer - outermost
        GQL[GraphQL\nQuery.cs Mutation.cs\nResolvers only]
        REST[REST Controllers\nAuthController HealthController]
    end

    subgraph Application Layer
        SVC[Services\nBusiness logic]
        INT[Interfaces\nContracts]
        DTO[DTOs\nInput and output shapes]
    end

    subgraph Domain Layer - innermost
        ENT[Entities\nIncome Expense Loan SavingsGoal]
        ENM[Enums\nLoanType]
        RES[AppResult\nResult pattern]
    end

    subgraph Infrastructure Layer
        REPO[Repositories\nDatabase access]
        CTX[AppDbContext\nEF Core]
        AUTH[SupabaseAuthClient\nHTTP calls]
    end

    GQL --> INT
    REST --> INT
    SVC --> ENT
    SVC --> INT
    REPO --> CTX
    CTX --> DB[(Supabase\nPostgreSQL)]
```

---

## The Dependency Rule

The most important rule in Clean Architecture:

> **Dependencies only point inward. Inner layers know nothing about outer layers.**

```mermaid
flowchart LR
    API[API Layer] --> APP[Application Layer]
    APP --> DOM[Domain Layer]
    INF[Infrastructure Layer] --> APP
    INF --> DOM

    DOM -. knows nothing about .-> APP
    DOM -. knows nothing about .-> INF
    DOM -. knows nothing about .-> API
    APP -. knows nothing about .-> INF
    APP -. knows nothing about .-> API
```

This means:
- `Income.cs` has zero imports — no EF Core, no GraphQL, no HTTP
- `IncomeService.cs` imports only Domain — no EF Core, no GraphQL
- `IncomeRepository.cs` imports Domain + Application interfaces — no GraphQL
- `Query.cs` imports Application interfaces — never Infrastructure directly

---

## How a Request Flows Through the Layers

Let's trace `addIncome` from the GraphQL request to the database and back.

```mermaid
sequenceDiagram
    participant Client
    participant Mutation as API Layer\nMutation.cs
    participant Service as Application Layer\nIncomeService.cs
    participant Repository as Infrastructure Layer\nIncomeRepository.cs
    participant DB as Supabase\nPostgreSQL

    Client->>Mutation: addIncome(input)\nBearer JWT

    Note over Mutation: Extract UserId from JWT\nNo business logic here

    Mutation->>Service: AddAsync(input, userId)

    Note over Service: Validate input\nBuild Income entity\nBusiness logic lives here

    Service->>Repository: AddAsync(income)

    Note over Repository: EF Core INSERT\nDatabase access only here

    Repository->>DB: INSERT INTO incomes
    DB-->>Repository: OK
    Repository-->>Service: saved
    Service-->>Mutation: AppResult.Ok(income)

    Note over Mutation: Map to IncomePayload\nNo business logic here

    Mutation-->>Client: IncomePayload { income, errors }
```

---

## What Each Layer Is Responsible For

### Domain Layer — the rules of the business

```csharp
// Income.cs — pure business rules, zero dependencies
public sealed class Income
{
    public Guid Id { get; }
    public decimal Amount { get; }

    public Income(Guid userId, decimal amount, string source, ...)
    {
        // Business invariants enforced here
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0.");

        Id = Guid.NewGuid();
        Amount = amount;
    }
}
```

The domain layer answers: **"What are the rules of this business?"**

### Application Layer — the use cases

```csharp
// IncomeService.cs — orchestrates domain + repository
public async Task<AppResult<Income>> AddAsync(AddIncomeInput input, Guid userId, ...)
{
    // Validate input (service-level validation)
    if (input.Amount <= 0)
        return AppResult<Income>.Fail("Amount must be greater than 0.");

    if (input.Date > DateTime.UtcNow)
        return AppResult<Income>.Fail("Date cannot be in the future.");

    // Build domain entity (domain-level validation)
    var income = new Income(userId, input.Amount, input.Source, input.Description, input.Date);

    // Persist via repository interface
    await repository.AddAsync(income, ct);

    return AppResult<Income>.Ok(income);
}
```

The application layer answers: **"What can users do with this system?"**

### Infrastructure Layer — the technical details

```csharp
// IncomeRepository.cs — EF Core, database, SQL
public async Task AddAsync(Income income, CancellationToken ct)
{
    await db.Incomes.AddAsync(income, ct);
    await db.SaveChangesAsync(ct);
}
```

The infrastructure layer answers: **"How do we store and retrieve data?"**

### API Layer — the delivery mechanism

```csharp
// Mutation.cs — GraphQL resolver, thin as possible
[Authorize]
public async Task<IncomePayload> AddIncome(
    AddIncomeInput input,
    [Service] IIncomeService service,
    ClaimsPrincipal claimsPrincipal,
    CancellationToken ct)
{
    var userId = claimsPrincipal.GetUserId();
    var result = await service.AddAsync(input, userId, ct);

    return result.IsSuccess
        ? new IncomePayload(result.Value, null)
        : new IncomePayload(null, [result.Error!]);
}
```

The API layer answers: **"How do clients communicate with the system?"**

---

## Why Resolvers Must Be Thin

A resolver that does too much:

```csharp
// BAD — business logic in resolver
[Authorize]
public async Task<IncomePayload> AddIncome(AddIncomeInput input, ...)
{
    // Validation in resolver — wrong layer
    if (input.Amount <= 0)
        return new IncomePayload(null, ["Amount must be greater than 0."]);

    // Direct DB access in resolver — wrong layer
    var income = new Income(userId, input.Amount, input.Source, input.Description, input.Date);
    await db.Incomes.AddAsync(income);
    await db.SaveChangesAsync();

    return new IncomePayload(income, null);
}
```

Problems:
- Cannot test without a real database
- Cannot reuse logic from another endpoint
- Validation duplicated if you add a REST endpoint later
- Violates Single Responsibility

A resolver that is correctly thin:

```csharp
// GOOD — resolver only wires things together
[Authorize]
public async Task<IncomePayload> AddIncome(AddIncomeInput input, ...)
{
    var userId = claimsPrincipal.GetUserId();
    var result = await service.AddAsync(input, userId, ct);

    return result.IsSuccess
        ? new IncomePayload(result.Value, null)
        : new IncomePayload(null, [result.Error!]);
}
```

---

## Interfaces — The Bridge Between Layers

Interfaces are what allow the dependency rule to work. The Application layer defines what it needs. The Infrastructure layer provides it.

```mermaid
flowchart LR
    subgraph Application defines the contract
        IREPO[IIncomeRepository\nGetAllAsync\nAddAsync\nDeleteAsync]
    end

    subgraph Infrastructure implements it
        REPO[IncomeRepository\nEF Core implementation]
    end

    subgraph Domain owns the entity
        ENT[Income entity]
    end

    IREPO -.implemented by.-> REPO
    IREPO --> ENT
    REPO --> ENT
```

The service only knows about `IIncomeRepository`. It has no idea whether the data comes from PostgreSQL, SQLite, or an in-memory list. This is what makes unit testing possible — you swap the real repository for a mock.

---

## DI — Dependency Injection Wires It All Together

In `Program.cs` we register everything:

```csharp
// Repositories — Infrastructure implements Application interfaces
builder.Services.AddScoped<IIncomeRepository, IncomeRepository>();

// Services — Application implements Application interfaces
builder.Services.AddScoped<IIncomeService, IncomeService>();
```

When Hot Chocolate calls the `AddIncome` resolver, it asks the DI container for `IIncomeService`. The container provides `IncomeService`, which was constructed with `IIncomeRepository`, which was constructed with `AppDbContext`. The whole chain is assembled automatically.

```mermaid
flowchart LR
    HC[Hot Chocolate\ncalls resolver]
    DI[DI Container]
    SVC[IncomeService]
    REPO[IncomeRepository]
    CTX[AppDbContext]

    HC --> DI
    DI --> SVC
    SVC --> REPO
    REPO --> CTX
```

---

## The AppResult Pattern — Errors Without Exceptions

Exceptions are for unexpected situations. Business failures are expected — they should be returned, not thrown.

```mermaid
flowchart TB
    subgraph Without AppResult
        W1[Service throws ValidationException]
        W2[Resolver catches exception]
        W3[Maps to error response]
        W4[Fragile - easy to forget catch]
        W1 --> W2 --> W3 --> W4
    end

    subgraph With AppResult
        A1[Service returns AppResult.Fail]
        A2[Resolver checks IsSuccess]
        A3[Maps to payload]
        A4[Compiler enforces handling]
        A1 --> A2 --> A3 --> A4
    end
```

```csharp
// AppResult forces the caller to handle both cases
public abstract class AppResultBase
{
    public string? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
}

public sealed class AppResult<T> : AppResultBase
{
    public T? Value { get; }

    public static AppResult<T> Ok(T value) => ...;
    public static AppResult<T> Fail(string error) => ...;
}
```

---

## How This Architecture Enables Testing

Because of Clean Architecture, each layer can be tested independently:

```mermaid
flowchart TB
    subgraph Unit Tests - no database
        UT[IncomeServiceTests\nMock IIncomeRepository\nTest business logic only]
    end

    subgraph Integration Tests - real database
        IT[IncomeRepositoryTests\nReal AppDbContext\nTest SQL queries]
    end

    subgraph End-to-End Tests - full stack
        ET[GraphQL resolver tests\nReal HTTP requests\nTest full flow]
    end
```

Unit tests mock the repository:

```csharp
// No database needed — mock returns fake data
var mockRepo = new Mock<IIncomeRepository>();
mockRepo.Setup(r => r.AddAsync(It.IsAny<Income>(), default))
        .Returns(Task.CompletedTask);

var service = new IncomeService(mockRepo.Object);
var result = await service.AddAsync(new AddIncomeInput(-100, "Salary", "", DateTime.UtcNow), userId);

Assert.True(result.IsFailure);
Assert.Equal("Amount must be greater than 0.", result.Error);
```

---

## Key Takeaways

| Concept | What it means |
|---|---|
| Clean Architecture | Strict layer separation — dependencies point inward only |
| Domain layer | Pure business rules — zero external dependencies |
| Application layer | Use cases — orchestrates domain and repositories |
| Infrastructure layer | Technical details — EF Core, HTTP clients, database |
| API layer | Delivery mechanism — GraphQL resolvers, REST controllers |
| Thin resolvers | Resolvers only extract UserId, call service, map result |
| Interfaces | Bridge between layers — enable testing and swapping implementations |
| DI | Wires all layers together at startup |
| AppResult | Business failures returned, not thrown — compiler-enforced handling |
