# Devcontainer Improvement Plan

## Purpose

This document defines the plan for turning the repository devcontainer into the standard development environment for the Finance Tracker backend.

The devcontainer should give every developer the same reliable workspace:
- same SDKs and CLI tools
- same editor behavior
- same test and debug workflow
- same database-testing capabilities
- same onboarding path

This is not just a convenience feature. It is part of the project architecture and part of the collaboration contract for the team.

## Mission

The devcontainer must be the default and supported way to work on this codebase.

A developer should be able to:
- open the repository in the devcontainer
- restore dependencies
- run the API
- run unit tests
- run Postgres-backed integration tests through Testcontainers
- debug from VS Code
- use the same scripts and tasks as the rest of the team

without manual machine-specific setup beyond Docker and the devcontainer host prerequisites.

## Current State

The repository already has:
- [devcontainer.json](/workspaces/finance-tracker-graphql/.devcontainer/devcontainer.json)
- [Dockerfile.Dev](/workspaces/finance-tracker-graphql/.devcontainer/Dockerfile.Dev)
- VS Code settings, tasks, and launch config under [.vscode](/workspaces/finance-tracker-graphql/.vscode)

Current strengths:
- .NET 10 SDK base image
- useful CLI tooling such as `gh`, `jq`, `rg`, `psql`
- basic VS Code extensions for C#
- restore command on container startup

Current gaps:
- no explicit Docker/Testcontainers contract for integration tests
- no environment validation step
- no standard secrets strategy for devcontainer users
- no formal onboarding script
- no documented port strategy for API, GraphQL, and future local services
- no shared development services composition for optional dependencies
- no standard task set for build, run, unit test, integration test, and formatting
- no clear policy for which `.vscode` files are committed and team-supported

## Target State

The target devcontainer experience should provide six guarantees.

### 1. Reproducible Toolchain

Every developer gets:
- .NET 10 SDK
- Node.js for supporting tooling
- PostgreSQL client tools
- GitHub CLI
- shell utilities used by the repo
- formatter and lint tooling required by the team

The image should be versioned and intentional, not an accidental collection of packages.

### 2. Test-Ready Environment

The devcontainer must support:
- Docker/Testcontainers access from inside the container
- unit test execution
- Postgres-backed integration test execution
- clear failure when Docker/Testcontainers access is not available

The team standard is:
- no external database path for automated integration tests
- integration tests run against disposable Testcontainers PostgreSQL only
- the devcontainer does not own or run a permanent shared Postgres container for automated tests
- the devcontainer provides the capability to launch disposable test databases on demand

SQL source of truth:
- SQL files required by the project stay under [scripts](/workspaces/finance-tracker-graphql/scripts)
- `.devcontainer/` may contain environment/bootstrap scripts, but not duplicated schema SQL
- test fixtures and bootstrap scripts may read from `scripts/` when SQL assets are needed

Optional compose-managed development services:
- `.devcontainer/docker-compose.yml` may provide manual developer convenience services
- automated integration tests must not depend on a compose-managed Postgres instance
- if a manual Postgres service is added for development convenience, use a predictable published port such as `55432:5432`
- application code inside the compose network should prefer service-name-based connections on port `5432`

### 3. Smooth Onboarding

A new contributor should need one path only:
1. open repository in devcontainer
2. wait for bootstrap
3. run the API or tests from documented tasks

The container should bootstrap:
- `dotnet restore`
- trusted local tool restore if added later
- validation of Docker/Testcontainers access
- validation of required secrets or env vars for auth flows

### 4. Shared Editor Workflow

The team should have committed and documented support for:
- extensions
- debugger launch profiles
- tasks for common commands
- formatting defaults
- test runner behavior

The goal is low-friction pairing and fewer “works on my machine” editor mismatches.

### 5. Safe Secrets and Config

The devcontainer plan should standardize:
- which secrets stay in environment variables
- which values may live in local-only files
- which sample files are committed
- which files must never be committed

The repo should prefer:
- checked-in templates such as `appsettings.Development.template.json` or `.env.example`
- secrets supplied through local env, devcontainer env injection, or platform secrets

### 6. Collaboration-First Defaults

The devcontainer should make collaboration easier by default:
- fast path for running tests before a PR
- consistent logs and problem matchers
- predictable ports
- shared scripts for setup and health checks
- minimal manual repair steps when someone joins the project

## Improvement Workstreams

### Workstream 1: Devcontainer Contract

Goal:
- define the devcontainer as the required development environment

Changes to plan:
- document required host prerequisites
- document Docker/Testcontainers requirement explicitly
- standardize container name and workspace folder behavior
- decide whether the container uses Docker socket passthrough or another supported approach

Done when:
- the team has a single documented supported setup
- integration test prerequisites are part of the devcontainer contract

### Workstream 2: Bootstrap and Validation

Goal:
- make startup self-checking instead of assuming the environment is healthy

Changes to plan:
- add a bootstrap script such as `scripts/devcontainer/bootstrap.sh`
- add an environment validation script such as `scripts/devcontainer/validate-env.sh`
- validate `dotnet --info`
- validate Docker/Testcontainers reachability
- validate repo restore success
- validate presence of required local auth config when relevant

Done when:
- opening the devcontainer gives fast success or actionable failure

### Workstream 3: Tooling and CLI Coverage

Goal:
- ensure the container includes every tool the team uses regularly

Candidates:
- `make` or `just` if the team wants command aliases
- `procps`, `less`, `tree`, `zip`
- `redis-cli` if cache verification is part of local workflows
- `dotnet-ef` local tool if migrations are part of daily work
- markdown lint or docs tooling if documentation becomes a first-class workflow

Done when:
- developers no longer need ad hoc host tools to contribute effectively

### Workstream 4: VS Code Team Workflow

Goal:
- make editor tasks first-class and shared

Changes to plan:
- add tasks for `restore`, `build`, `run`, `watch`, `unit tests`, `integration tests`, and `format`
- add a dedicated launch profile for the API with clear port expectations
- decide which `.vscode` files remain committed
- align problem matchers and test discovery behavior

Done when:
- the expected daily commands are available from the command palette and task runner

### Workstream 5: Secrets and Local Config Hygiene

Goal:
- remove ambiguity around local secrets

Changes to plan:
- add committed template files for local config
- document where real secrets come from
- ensure real credentials are never required in committed files
- document how auth-related local development works inside the devcontainer

Done when:
- a developer can configure the app without guessing where secrets belong

### Workstream 6: Service Topology

Goal:
- define which supporting services the devcontainer should know about

Questions to settle:
- should the API still run against Supabase for normal development?
- should Redis be optional or available locally for cache-focused work?
- should we add a `docker-compose` file for optional companion services used during development?

Recommended direction:
- keep the application runtime config flexible
- keep automated integration tests on Testcontainers
- do not add a long-lived Postgres test service to the devcontainer for automated tests
- allow an optional compose-managed Postgres service only for manual development workflows
- if that optional Postgres service exists, standardize its published port as `55432`
- keep project SQL assets centralized under `scripts/`
- avoid turning the main dev flow into a pile of manual sidecar services unless they are truly needed

Done when:
- the local service story is documented and consistent

## Recommended File Additions

These are the most likely additions for the implementation phase:
- `.devcontainer/postCreate.sh`
- `.devcontainer/postStart.sh`
- `scripts/devcontainer/bootstrap.sh`
- `scripts/devcontainer/validate-env.sh`
- `scripts/devcontainer/check-testcontainers.sh`
- `appsettings.Development.template.json`
- `docs/devcontainer-plan.md`

Possible additions depending on chosen direction:
- `.devcontainer/docker-compose.yml`
- `.devcontainer/devcontainer.env.example`
- `.config/dotnet-tools.json`

If `.devcontainer/docker-compose.yml` is introduced, its role should be limited to optional manual development services, not automated integration test infrastructure.

## Proposed Rollout

### Phase 1: Hardening

Focus:
- validate the current devcontainer
- add bootstrap and validation scripts
- document Docker/Testcontainers requirement
- document that automated tests use disposable Postgres containers, not a permanent devcontainer database
- add missing VS Code tasks for tests and formatting

Success criteria:
- a developer can open the devcontainer and run unit and integration tests without custom repair work

## Test Suite Alignment

The devcontainer and testing architecture should improve specific existing test suites, not just the environment around them.

### Current Unit Tests To Keep and Harden

These suites already fit the architecture and should remain fast, deterministic, and dependency-free:
- [AuthServiceSignInTests.cs](/workspaces/finance-tracker-graphql/Tests/UnitTests/Auth/AuthServiceSignInTests.cs)
- [AuthServiceSignUpTests.cs](/workspaces/finance-tracker-graphql/Tests/UnitTests/Auth/AuthServiceSignUpTests.cs)
- [DashboardServiceCacheTests.cs](/workspaces/finance-tracker-graphql/Tests/UnitTests/Dashboard/DashboardServiceCacheTests.cs)
- [DashboardServiceSummaryTests.cs](/workspaces/finance-tracker-graphql/Tests/UnitTests/Dashboard/DashboardServiceSummaryTests.cs)
- [ExpenseServiceTests.cs](/workspaces/finance-tracker-graphql/Tests/UnitTests/Expense/ExpenseServiceTests.cs)
- [IncomeServiceTests.cs](/workspaces/finance-tracker-graphql/Tests/UnitTests/Income/IncomeServiceTests.cs)
- [LoanServiceTests.cs](/workspaces/finance-tracker-graphql/Tests/UnitTests/Loan/LoanServiceTests.cs)
- [SavingsGoalServiceTests.cs](/workspaces/finance-tracker-graphql/Tests/UnitTests/Savings/SavingsGoalServiceTests.cs)

Improvements planned:
- make `Trait` usage consistent across all unit suites
- add any missing case IDs where numbering is sparse or inconsistent
- keep builders and common helpers centralized under `Tests/UnitTests/Builders` and `Tests/UnitTests/Common`
- add task support in the devcontainer for running the full unit suite and feature-scoped unit runs

### Current Integration Tests To Harden Under the New Model

These suites should remain Postgres-backed and run through Testcontainers from inside the devcontainer:
- [DashboardCacheServiceTests.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Caching/DashboardCacheServiceTests.cs)
- [DashboardRepositoryTests.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Dashboard/DashboardRepositoryTests.cs)
- [ExpenseRepositoryTests.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Expense/ExpenseRepositoryTests.cs)
- [IncomeRepositoryTests.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Income/IncomeRepositoryTests.cs)
- [LoanRepaymentRepositoryTests.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Loan/LoanRepaymentRepositoryTests.cs)
- [LoanRepositoryTests.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Loan/LoanRepositoryTests.cs)
- [SavingsGoalRepositoryTests.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Savings/SavingsGoalRepositoryTests.cs)

Fixture and infrastructure files to improve:
- [PostgresDatabaseFixture.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Common/PostgresDatabaseFixture.cs)
- [RepositoryIntegrationTestBase.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Common/RepositoryIntegrationTestBase.cs)
- [IntegrationFactAttribute.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Common/IntegrationFactAttribute.cs)
- [DockerAvailability.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Common/DockerAvailability.cs)
- [PostgresCollection.cs](/workspaces/finance-tracker-graphql/Tests/IntegrationTests/Common/PostgresCollection.cs)

Improvements planned:
- replace skip-oriented local assumptions with devcontainer-first environment validation
- ensure failures clearly distinguish environment setup issues from test failures
- add stronger fixture diagnostics for Testcontainers startup and database reset
- keep all DB-backed suites aligned with the `scripts/` SQL ownership model
- add devcontainer tasks for running the full integration suite and selected feature suites

### New API-Level Tests To Add

To complete the new architecture, add an API integration layer focused on GraphQL and REST pipeline behavior.

Recommended new areas:
- `Tests/ApiTests/GraphQL/Auth`
- `Tests/ApiTests/GraphQL/Income`
- `Tests/ApiTests/GraphQL/Expense`
- `Tests/ApiTests/GraphQL/Savings`
- `Tests/ApiTests/GraphQL/Loan`
- `Tests/ApiTests/GraphQL/Dashboard`
- `Tests/ApiTests/Rest/AuthControllerTests.cs`
- `Tests/ApiTests/Rest/HealthControllerTests.cs`

Recommended first test cases:
- public GraphQL `health` query succeeds without JWT
- protected GraphQL operations reject missing JWTs
- protected GraphQL operations reject invalid JWTs
- successful mutations return expected payloads
- query filters behave correctly for income, expense, loan, and dashboard operations
- GraphQL error handling returns safe, consistent responses
- REST auth endpoints return expected status codes and body shapes

### Completion Standard For This Rollout

The new architecture is solid when:
- current unit suites run cleanly in the devcontainer
- current Postgres-backed integration suites run through Testcontainers in the devcontainer
- environment problems fail fast and clearly
- API-level integration tests exist for auth, health, GraphQL authorization, payloads, and filtering
- developer workflows for `unit`, `integration`, and `api` tests are available as shared tasks

### Phase 2: Collaboration Defaults

Focus:
- standardize secrets templates
- standardize debug profiles
- standardize committed workspace settings
- improve onboarding documentation

Success criteria:
- a new contributor can get productive from a clean checkout with minimal help

### Phase 3: Optional Service Enhancements

Focus:
- evaluate Redis local workflow
- evaluate auxiliary compose services
- evaluate EF migration tooling support

Success criteria:
- optional services help specialized workflows without making the normal flow heavier
- optional compose-managed Postgres, if added, is clearly documented as manual-development-only and uses the agreed port convention

## Acceptance Criteria

The devcontainer work is successful when all of the following are true:

- `dotnet restore` succeeds from a clean devcontainer start
- `dotnet test Tests/UnitTests/UnitTests.csproj` succeeds
- `dotnet test Tests/IntegrationTests/IntegrationTests.csproj` can run through Testcontainers from inside the devcontainer
- failure to access Docker/Testcontainers is reported as an environment issue
- the API can be launched and debugged from VS Code
- the required extensions and tasks are present and documented
- no real secrets are required in committed files
- project SQL assets remain centralized under `scripts/`
- `.devcontainer/` does not become a second source of truth for database schema SQL
- optional compose-managed Postgres, if present, is not used by automated integration tests
- onboarding steps fit comfortably in the README plus this plan

## Recommended Next Implementation Order

1. Add bootstrap and validation scripts.
2. Expand VS Code tasks to cover the full daily workflow.
3. Add template config files for local secrets and app settings.
4. Update the devcontainer to validate Docker/Testcontainers access on startup.
5. Decide whether optional local Redis support is worth adding.

## Notes for This Repository

Two repo-specific details are worth handling carefully during implementation:

- The project already treats devcontainers as part of the testing architecture through the Testcontainers requirement documented in [test-requirements.md](/workspaces/finance-tracker-graphql/docs/test-requirements.md).
- The repository currently tracks some `.vscode` files while `.gitignore` also ignores `.vscode/`, so we should explicitly decide which workspace files are team-owned and supported.
