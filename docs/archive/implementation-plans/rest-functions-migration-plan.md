# REST + Azure Functions Migration Plan

## Decision

Move the production API on `main` from GraphQL on Azure Container Apps to REST on Azure Functions Flex Consumption.

Keep the GraphQL implementation for learning and reference on a separate branch:

```bash
git switch main
git pull
git branch graphql-learning
git push origin graphql-learning
```

This keeps the production path simpler and cheaper while preserving the GraphQL work.

## References Reviewed

- Current production codebase: ASP.NET Core host, Hot Chocolate GraphQL, REST auth and health controllers, Azure Container Apps scripts.
- Reference repo: `https://github.com/shipht-it/lh-billing`
  - .NET isolated Azure Functions host in `Agile/Generators/agile-invoicegenerator/Program.cs`
  - Function project shape in `Agile/Generators/agile-invoicegenerator/agile-invoicegenerator.csproj`
  - `host.json` HTTP route prefix and Application Insights setup
  - Flex Consumption deployment script in `Agile/Generators/agile-invoicegenerator/Infrastructure/Azure/1.AzureFunction/1.deploy-agile-invoicegenerator-AzFunc.sh`
  - GitHub OIDC deploy workflow in `.github/workflows/dev-deploy-agile-invoicegenerator.yml`
  - CI pattern in `.github/workflows/pr-ci.yml`
- Microsoft Azure Functions docs:
  - .NET isolated worker model for modern .NET Azure Functions
  - Flex Consumption hosting for scale-to-zero and serverless billing

## Target Architecture

Keep the existing business layers:

```text
Domain/
Application/
Infrastructure/
```

Replace the ASP.NET Core web host surface with Azure Functions HTTP triggers:

```text
Api/Functions/
  HealthFunction.cs
  AuthFunctions.cs
  IncomeFunctions.cs
  ExpenseFunctions.cs
  SavingsGoalFunctions.cs
  LoanFunctions.cs
  DashboardFunctions.cs

Api/Functions/Support/
  FunctionJson.cs
  FunctionAuth.cs
  FunctionHttpResults.cs
  FunctionQuery.cs
```

Use versioned REST routes:

```text
/api/v1/health
/api/v1/auth/signup
/api/v1/auth/signin
/api/v1/incomes
/api/v1/expenses
/api/v1/savings-goals
/api/v1/loans
/api/v1/dashboard
```

Do not version GraphQL on `main`; remove it from production after the branch is created.

## Endpoint Mapping

| Current GraphQL/REST surface | Target REST endpoint |
| --- | --- |
| `GET /health` | `GET /api/v1/health` |
| `POST /api/auth/signup` | `POST /api/v1/auth/signup` |
| `POST /api/auth/signin` | `POST /api/v1/auth/signin` |
| `Query.incomes(fromDate,toDate,source)` | `GET /api/v1/incomes?fromDate=&toDate=&source=` |
| `Query.income(id)` | `GET /api/v1/incomes/{id}` |
| `Mutation.addIncome` | `POST /api/v1/incomes` |
| `Mutation.updateIncome` | `PUT /api/v1/incomes/{id}` |
| `Mutation.deleteIncome` | `DELETE /api/v1/incomes/{id}` |
| `Query.expenses(fromDate,toDate,category)` | `GET /api/v1/expenses?fromDate=&toDate=&category=` |
| `Query.expense(id)` | `GET /api/v1/expenses/{id}` |
| `Mutation.addExpense` | `POST /api/v1/expenses` |
| `Mutation.updateExpense` | `PUT /api/v1/expenses/{id}` |
| `Mutation.deleteExpense` | `DELETE /api/v1/expenses/{id}` |
| `Query.savingsGoals` | `GET /api/v1/savings-goals` |
| `Query.savingsGoal(id)` | `GET /api/v1/savings-goals/{id}` |
| `Mutation.createSavingsGoal` | `POST /api/v1/savings-goals` |
| `Mutation.updateSavingsGoal` | `PUT /api/v1/savings-goals/{id}` |
| `Mutation.addSavingsContribution` | `POST /api/v1/savings-goals/{id}/contributions` |
| `Mutation.updateSavingsProgress` | `PATCH /api/v1/savings-goals/{id}/progress` |
| `Mutation.deleteSavingsGoal` | `DELETE /api/v1/savings-goals/{id}` |
| `Query.loans(type,status)` | `GET /api/v1/loans?type=&status=` |
| `Query.loan(id)` | `GET /api/v1/loans/{id}` |
| `Mutation.addLoan` | `POST /api/v1/loans` |
| `Mutation.updateLoan` | `PUT /api/v1/loans/{id}` |
| `Mutation.recordRepayment` | `POST /api/v1/loans/{id}/repayments` |
| `Mutation.settleLoan` | `POST /api/v1/loans/{id}/settle` |
| `Mutation.deleteLoan` | `DELETE /api/v1/loans/{id}` |
| `Query.loanRepaymentHistory(loanId)` | `GET /api/v1/loans/{id}/repayments` |
| `Query.dashboard(fromDate,toDate,activityLimit)` | `GET /api/v1/dashboard?fromDate=&toDate=&activityLimit=` |

## Codebase Changes

### Branching

- Create and push `graphql-learning` before deleting GraphQL code from `main`.
- Keep `main` as the production REST + Functions branch.

### Project Host

Change `FinanceTracker.csproj` from ASP.NET Core Web SDK to Azure Functions isolated worker:

- Replace `Microsoft.NET.Sdk.Web` with `Microsoft.NET.Sdk`.
- Add:
  - `AzureFunctionsVersion` = `V4`
  - `OutputType` = `Exe`
- Remove production GraphQL packages:
  - `HotChocolate.AspNetCore`
  - `HotChocolate.Authorization`
- Remove ASP.NET Core web-only telemetry package:
  - `Azure.Monitor.OpenTelemetry.AspNetCore`
- Add Functions packages:
  - `Microsoft.Azure.Functions.Worker`
  - `Microsoft.Azure.Functions.Worker.Sdk`
  - `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore`
  - `Microsoft.Azure.Functions.Worker.ApplicationInsights`
  - `Microsoft.ApplicationInsights.WorkerService`
- Keep:
  - `Azure.Identity`
  - `Azure.Security.KeyVault.Secrets`
  - `Microsoft.Extensions.Caching.StackExchangeRedis`
  - `Microsoft.EntityFrameworkCore`
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - JWT token validation packages already used by auth.

### Host Bootstrap

Replace `Program.cs` with an isolated worker bootstrap similar to `lh-billing`:

- Use `FunctionsApplication.CreateBuilder(args)`.
- Call `builder.ConfigureFunctionsWebApplication()`.
- Load Key Vault before registering services.
- Register Application Insights worker telemetry.
- Register existing services:
  - `AddApiAuthCore` or equivalent token validation support
  - `AddSupabaseClient`
  - `AddDatabase`
  - `AddDashboardCaching`
  - `AddRepositories`
  - `AddServices`
- Remove:
  - `app.UseAuthentication()`
  - `app.UseAuthorization()`
  - `app.MapControllers()`
  - `app.MapGraphQL()`
  - ASP.NET Core middleware pipeline assumptions.

### Configuration

Add:

```text
host.json
local.settings.json.template
```

Borrow from `lh-billing`:

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true
    }
  },
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  },
  "functionTimeout": "00:05:00"
}
```

Use app settings:

```text
ASPNETCORE_ENVIRONMENT=Production
FUNCTIONS_ENVIRONMENT=Production
AzureKeyVault__VaultUri=https://kv-fintrack-dev-neu.vault.azure.net/
AzureKeyVault__CredentialMode=ManagedIdentity
AzureWebJobsStorage=<Function runtime storage connection string>
```

Local development can use:

```text
AzureKeyVault__CredentialMode=AzureCli
```

### REST Function Files

Create:

```text
Api/Functions/HealthFunction.cs
Api/Functions/AuthFunctions.cs
Api/Functions/IncomeFunctions.cs
Api/Functions/ExpenseFunctions.cs
Api/Functions/SavingsGoalFunctions.cs
Api/Functions/LoanFunctions.cs
Api/Functions/DashboardFunctions.cs
```

Function style:

```csharp
[Function("GetIncomes")]
public async Task<IActionResult> GetIncomesAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/incomes")]
    HttpRequestData req,
    CancellationToken ct)
```

Use `AuthorizationLevel.Anonymous` because Supabase/JWT bearer auth is app-level. Each protected function should call a shared auth helper that:

- Reads `Authorization: Bearer <token>`.
- Validates token with the same signing key semantics as current `AuthExtensions`.
- Extracts the user id from `sub`.
- Returns `401` for missing/invalid/expired tokens.

Create:

```text
Api/Functions/Support/FunctionAuth.cs
```

This replaces `[Authorize]` attributes used by GraphQL.

### JSON and HTTP Helpers

Create:

```text
Api/Functions/Support/FunctionJson.cs
Api/Functions/Support/FunctionHttpResults.cs
Api/Functions/Support/FunctionQuery.cs
```

Responsibilities:

- Deserialize request bodies.
- Parse `DateTime`, `Guid`, enum, pagination and optional filters.
- Convert `Result<T>` failures to consistent REST responses.
- Return:
  - `400` validation errors
  - `401` auth errors
  - `404` not found
  - `409` conflict where applicable
  - `500` unexpected failures after logging at the function boundary.

Borrow the explicit query parsing style from `lh-billing` function files instead of relying on model binding magic.

### Remove GraphQL From Production

After `graphql-learning` is pushed, delete or exclude from `main`:

```text
Api/GraphQL/
Api/Extensions/GraphQlExtensions.cs
Infrastructure/Auth/GraphQlAuthorizationHandler.cs
```

Update `Program.cs` and `FinanceTracker.csproj` accordingly.

### Controllers

Remove or convert:

```text
Api/Controllers/AuthController.cs
Api/Controllers/HealthController.cs
```

The Function equivalents replace them:

```text
Api/Functions/AuthFunctions.cs
Api/Functions/HealthFunction.cs
```

### Authentication

Refactor:

```text
Api/Extensions/AuthExtensions.cs
Infrastructure/Auth/ApiTokenService.cs
Infrastructure/Auth/JwtSigningKeyFactory.cs
```

Target:

- Keep `ApiTokenService.CreateAccessToken`.
- Add a token validator service:

```text
Application/Interfaces/IApiTokenValidator.cs
Infrastructure/Auth/ApiTokenValidator.cs
```

`ApiTokenValidator` should return `ClaimsPrincipal` or a typed auth result.

This lets Azure Functions validate the same JWTs without ASP.NET Core auth middleware.

### API Versioning

Use route-based versioning manually in Functions:

```text
Route = "v1/incomes"
```

Because `host.json` sets `routePrefix` to `api`, the public route becomes:

```text
/api/v1/incomes
```

Add a future-proof constant:

```text
Api/Functions/Support/ApiRoutes.cs
```

### Tests

Update unit tests to keep testing services directly:

```text
Tests/UnitTests/**
```

Add function boundary tests:

```text
Tests/UnitTests/Functions/AuthFunctionsTests.cs
Tests/UnitTests/Functions/IncomeFunctionsTests.cs
Tests/UnitTests/Functions/ExpenseFunctionsTests.cs
Tests/UnitTests/Functions/SavingsGoalFunctionsTests.cs
Tests/UnitTests/Functions/LoanFunctionsTests.cs
Tests/UnitTests/Functions/DashboardFunctionsTests.cs
```

Integration tests can remain repository/service-focused at first:

```text
Tests/IntegrationTests/**
```

Later, add a Functions host E2E suite only if needed. Borrow `lh-billing` lessons:

- Avoid parallel `func start` hosts.
- Use a shared E2E test collection.
- Treat any non-404 response from a route as host readiness.
- Capture host logs when startup fails.

### Documentation

Update:

```text
README.md
docs/e2e-quickstart.md
docs/postman/test-examples.json
docs/phases/phase-04-graphql-api.md
docs/azure-key-vault-migration-plan.md
Infrastructure/Azure/README.md
```

Add:

```text
docs/rest-api.md
docs/functions-local-development.md
docs/functions-deployment-runbook.md
```

Mark old GraphQL docs as learning-branch only.

## Azure Infrastructure Changes

Add a new folder:

```text
Infrastructure/Azure/AzureFunctions/
  1.deploy-financetracker-AzFunc.sh
  2.teardown-financetracker-AzFunc.sh
  3.update-financetracker-AzFunc.sh
  README.md
```

Borrow from `lh-billing`:

- `--plan` mode for deployment preview.
- Idempotent resource creation.
- Flex Consumption Function App.
- Dedicated Function runtime storage account.
- Managed identity assignment.
- Key Vault access setup.
- Generated GitHub Actions workflow.
- GitHub OIDC instead of long-lived Azure credentials.
- Post-deploy summary and next steps.

Target resources:

```text
Function App       : func-fintrack-api-dev-neu-001
Runtime            : dotnet-isolated 10.0
Functions version  : 4
Plan               : Flex Consumption
Storage account    : stfintrackfuncdevneu001 or shortened globally unique equivalent
Key Vault          : kv-fintrack-dev-neu
Log Analytics      : log-fintrack-api-dev-neu-001
Application Insights: appi-fintrack-api-dev-neu-001
```

Important: storage account names must be globally unique, lowercase alphanumeric, and at most 24 characters.

The Function App deploy script should configure:

```text
AzureWebJobsStorage
ASPNETCORE_ENVIRONMENT
FUNCTIONS_ENVIRONMENT
AzureKeyVault__VaultUri
AzureKeyVault__CredentialMode
APPLICATIONINSIGHTS_CONNECTION_STRING
```

For Key Vault access, prefer RBAC because the current project already uses RBAC:

```bash
az role assignment create \
  --assignee "$principal_id" \
  --role "Key Vault Secrets User" \
  --scope "$kv_id"
```

## GitHub Actions Changes

Remove after Function App cutover:

```text
.github/workflows/ca-fintrack-api-dev-neu-001-AutoDeployTrigger-*.yml
```

Add:

```text
.github/workflows/dev-deploy-fintrack-api-functions.yml
```

Workflow shape borrowed from `lh-billing`:

```yaml
permissions:
  id-token: write
  contents: read

steps:
  - uses: actions/checkout@v4
  - uses: actions/setup-dotnet@v4
    with:
      global-json-file: global.json
  - uses: actions/setup-node@v4
    with:
      node-version: '20.x'
  - run: npm i -g azure-functions-core-tools@4 --unsafe-perm true
  - run: |
      dotnet restore FinanceTracker.csproj
      dotnet restore Tests/UnitTests/UnitTests.csproj
      dotnet restore Tests/IntegrationTests/IntegrationTests.csproj
  - run: dotnet build FinanceTracker.csproj --configuration Release --no-restore
  - run: |
      dotnet test Tests/UnitTests/UnitTests.csproj --configuration Release --no-build --verbosity normal
      dotnet test Tests/IntegrationTests/IntegrationTests.csproj --configuration Release --no-build --verbosity normal
  - run: dotnet publish FinanceTracker.csproj --configuration Release --output ./publish
  - uses: azure/login@v2
    with:
      client-id: ${{ secrets.AZURE_CLIENT_ID }}
      tenant-id: ${{ secrets.AZURE_TENANT_ID }}
      subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
  - uses: Azure/functions-action@v1
    with:
      app-name: func-fintrack-api-dev-neu-001
      package: ./publish
```

Also add a smoke workflow:

```text
.github/workflows/dev-smoke-fintrack-api-functions.yml
```

Smoke checks:

```bash
curl -fsS "$FUNCTION_BASE_URL/api/v1/health"
```

Optional authenticated smoke:

- Sign in through `/api/v1/auth/signin`.
- Call `/api/v1/dashboard`.

## Cutover Plan

1. Create and push `graphql-learning`.
2. Convert `main` to REST Functions locally.
3. Run:

```bash
dotnet test vela.sln
func start
curl http://localhost:7071/api/v1/health
```

4. Deploy Function App to Azure.
5. Smoke test Function App URL.
6. Keep Container App running for one release window as rollback.
7. Update clients to use Function App URL.
8. Disable Container App auto-deploy workflow.
9. Stop or scale down Container App.
10. After confidence window, tear down Container App resources with the existing teardown script.

## Rollback Plan

Until the Container App is deleted:

- Repoint clients back to:

```text
https://ca-fintrack-api-dev-neu-001.graypebble-a19184aa.northeurope.azurecontainerapps.io
```

- Re-enable the Container Apps GitHub workflow if it was disabled.
- Keep `graphql-learning` untouched so GraphQL code is recoverable.

## Implementation Order

### Phase 0: Branch Safety

- [ ] Create `graphql-learning`.
- [ ] Confirm `main` is clean and current.

### Phase 1: REST Contract

- [ ] Add `docs/rest-api.md`.
- [ ] Define request/response examples for all `/api/v1` endpoints.
- [ ] Update Postman examples.

### Phase 2: Function Host

- [ ] Convert `FinanceTracker.csproj`.
- [ ] Replace `Program.cs`.
- [ ] Add `host.json`.
- [ ] Add `local.settings.json.template`.
- [ ] Keep all existing service registrations working.

### Phase 3: Auth

- [ ] Add `IApiTokenValidator`.
- [ ] Add `ApiTokenValidator`.
- [ ] Add `FunctionAuth`.
- [ ] Update auth tests.

### Phase 4: REST Functions

- [ ] Add health and auth functions.
- [ ] Add income functions.
- [ ] Add expense functions.
- [ ] Add savings goal functions.
- [ ] Add loan functions.
- [ ] Add dashboard function.
- [ ] Remove controllers from production host.

### Phase 5: Remove GraphQL From `main`

- [ ] Remove GraphQL packages.
- [ ] Remove `Api/GraphQL`.
- [ ] Remove GraphQL auth handler.
- [ ] Remove GraphQL docs from production README.

### Phase 6: Azure Functions Infrastructure

- [ ] Add `Infrastructure/Azure/AzureFunctions/1.deploy-financetracker-AzFunc.sh`.
- [ ] Add Function App storage provisioning.
- [ ] Add Function App Flex Consumption provisioning.
- [ ] Add managed identity and Key Vault RBAC.
- [ ] Add generated OIDC GitHub workflow.
- [ ] Add teardown/update scripts.

### Phase 7: CI/CD

- [ ] Update CI to build/test Function host.
- [ ] Add Function App deployment workflow.
- [ ] Add smoke workflow.
- [ ] Remove Container App workflow after cutover.

### Phase 8: Production Cutover

- [ ] Deploy Function App.
- [ ] Verify `/api/v1/health`.
- [ ] Verify auth sign-in.
- [ ] Verify one protected endpoint.
- [ ] Update clients.
- [ ] Stop Container App after confidence window.

## Risks

| Risk | Mitigation |
| --- | --- |
| Azure Functions auth does not use ASP.NET middleware | Add explicit `ApiTokenValidator` and `FunctionAuth` helper. |
| Cold starts | Use Flex Consumption; consider one always-ready instance only if latency matters. |
| Function storage account naming conflicts | Generate compact globally unique names and check before create. |
| Key Vault startup failures | Keep required-secret validation with actionable errors. |
| GraphQL deletion loses learning context | Push `graphql-learning` before deleting anything on `main`. |
| Generated workflow drift | Prefer a repo-owned workflow model like `lh-billing`, not Azure-generated Container Apps workflow. |

## Borrowed Patterns From `lh-billing`

- .NET isolated worker instead of in-process Functions.
- `FunctionsApplication.CreateBuilder(args)` bootstrap.
- `host.json` with `routePrefix: api`.
- Explicit HTTP trigger route definitions.
- Manual query parsing for predictable validation.
- Idempotent shell deploy scripts with `--plan`.
- Flex Consumption creation through `az functionapp create`.
- Managed identity plus Key Vault access.
- GitHub OIDC workflow instead of storing Azure client secrets.
- Narrow deploy workflow paths.
- Separate live smoke workflow after deploy.
