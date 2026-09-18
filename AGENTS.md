# Lutra Repository Instructions

## Working Directory

- The solution and all .NET projects are under `Lutra/`; run `dotnet` and Docker commands from that directory unless a command says otherwise.

## Structure

- This is a .NET 10 Clean Architecture solution. `Lutra.Domain` has no project dependencies; `Lutra.Application` depends on Domain; the `Lutra.Infrastructure` solution folder holds `Lutra.Infrastructure.Sql` (EF Core persistence) and `Lutra.Infrastructure.OpenRouter` (retail-page fetching and OpenRouter extraction, referencing Application only); `Lutra.API` depends on Application and both Infrastructure projects.
- Application use cases use `Cortex.Mediator` and are organized as feature partials (`FeatureName.Query/Command`, `Handler`, `Response`); keep controllers thin and delegate to the mediator.
- Durable background work uses the `BackgroundCommandJob` entity with `BackgroundCommandProcessor`/`BackgroundCommandWorker`; `POST /api/verspakketten/import` queues an `ImportVerspakket` job and `GET /api/background-commands/{id}` reports its status.
- `Lutra.AppHost` is Aspire orchestration only. `Lutra.Infrastructure.Migrator` is a one-shot host that applies EF migrations and reconciles the seed data.

## General Standards

- Prefer small, focused changes and follow existing naming, formatting, and folder conventions.
- Keep dependencies pointing inward: Domain depends on nothing; Application depends on Domain; Infrastructure depends on Application and Domain; API depends on Application and Infrastructure; AppHost and Migrator are host projects only.
- Use nullable reference types correctly and keep implicit usings compatible with the project style.

## Domain and Application

- Keep domain entities simple and focused on business state. Use `BaseEntity` where appropriate; entities use `Guid` IDs, audit fields, and derived `IsDeleted`.
- Keep validation and persistence concerns out of Domain unless they are intrinsic business rules.
- Put use cases in feature folders using the existing partial split: `FeatureName.cs`, `FeatureName.Query.cs` or `FeatureName.Command.cs`, `FeatureName.Handler.cs`, and `FeatureName.Response.cs`.
- Use `Cortex.Mediator` request and handler interfaces. Keep handlers focused on orchestration and application logic.
- Use `ILutraDbContext` rather than the concrete DbContext in application handlers.
- Put shared DTOs and enums that are not use-case-specific in `Models/<FeatureArea>/`.

## Infrastructure and API

- Put EF Core implementation, migrations, and database wiring in `Lutra.Infrastructure` (the `Lutra.Infrastructure.Sql` project); keep its DbContext aligned with `ILutraDbContext`.
- Pin infrastructure package versions explicitly when runtime compatibility requires it. Be cautious with design-time packages using `PrivateAssets="all"`, since they do not flow dependency constraints downstream.
- Keep controllers thin and delegate to Application use cases through `IMediator`; keep `Program.cs` focused on DI and middleware setup.

## API Hardening and Error Handling

- Application handlers own input bounds and throw typed exceptions; `ExceptionHandlingMiddleware` maps them to RFC 7807 problems (`ValidationException` 400, `NotFoundException` 404, `ConflictException` 409, `UnprocessableException` 422, `TooManyRequestsException` 429, `ExternalServiceException` 502, anything else 500). Do not echo `ExternalServiceException` details to clients; they are logged instead.
- Keep the existing bounds intact when touching related code: list endpoints clamp `skip`/`take` (`GetVerspakketten`/`GetSupermarkten`, max 200 per page), base64 photos go through `VerspakketFotoValidator` (max 10 photos, 5 MiB each, 20 MiB total), the import queue is capped by `BackgroundCommands:MaxPendingJobs`, and `POST /api/verspakketten/import` is rate limited per IP (`verspakket-import` policy in `Program.cs`).
- `Program.cs` enables HSTS outside Development and caps request bodies at 32 MiB; the retail `HttpClient` uses `PublicNetworkHttpHandler` for SSRF protection via `OpenRouter:AllowedHosts`/`AllowedImageHosts`.
- The OpenRouter integration is configured under the `OpenRouter` section. Never commit a real API key; supply `OpenRouter:ApiKey` through user-secrets or environment variables (`OpenRouter__ApiKey`).

## API Documentation

- `docs/api.md` at the repository root is the single source of truth for the HTTP API. Whenever the API surface changes—new or renamed endpoints, changed request or response shapes, new or changed enum values, changed validation rules, limits, rate limits, status codes/error mapping, or background-command behavior—update `docs/api.md` in the same change.
- New or changed endpoints must include a worked example: a curl invocation plus the request and response JSON (success and relevant error cases), and the enums, limits, and status-code reference tables must be kept in sync with the Application/Domain enums and the `ExceptionHandlingMiddleware` mapping.
- `README.md` links to `docs/api.md` as the API reference; keep the README's setup and configuration sections consistent with the contracts in `docs/api.md`.
- Interactive OpenAPI docs (Scalar) are served by the API in Development at `/scalar/v1` (OpenAPI at `/openapi/v1.json`); the human-readable `docs/api.md` must not drift from that generated output.

## Aspire and Migration Rules

- Keep `Lutra.AppHost` responsible for orchestration only and preserve the persistent PostgreSQL container unless the change explicitly requires otherwise.
- Keep the migrator as a one-shot project that applies migrations on startup. Ensure migration changes do not break runtime assembly/version consistency.

## Commands

Run from `Lutra/`:

```bash
dotnet restore Lutra.sln
dotnet build Lutra.sln
dotnet run --project Lutra.Application.UnitTests/Lutra.Application.UnitTests.csproj
dotnet run --project Lutra.Infrastructure.OpenRouter.UnitTests/Lutra.Infrastructure.OpenRouter.UnitTests.csproj
dotnet run --project Lutra.API.IntegrationTests/Lutra.API.IntegrationTests.csproj
dotnet run --project Lutra.API.IntegrationTests/Lutra.API.IntegrationTests.csproj -- -class "*VerspakkettenControllerTests*"
dotnet run --project Lutra.AppHost/Lutra.AppHost.csproj
```

- The test projects use xUnit v3 on Microsoft.Testing.Platform, so run them directly with `dotnet run`. `dotnet test` fails on the .NET 10 SDK ("Testing with VSTest target is no longer supported") unless the new dotnet test experience is opted in; filtering uses xUnit flags such as `-class`, `-method`, `-namespace`, or `-trait` after `--`.
- Integration tests do not require PostgreSQL: `LutraApiFactory` replaces the API's Npgsql registration with one shared open SQLite in-memory connection and creates the schema with `EnsureCreated`; tests reset data after each test through `IntegrationTestBase`.
- Running the API directly uses PostgreSQL and requires `ConnectionStrings:LutraDb` (or environment variable `ConnectionStrings__LutraDb`); the Development settings contain a placeholder password.
- Aspire provisions a persistent PostgreSQL container, creates `LutraDb`, waits for the migrator to finish, then starts the API. Docker must be available for the AppHost flow.

## Persistence Changes

- EF Core implementation and migrations belong in `Lutra.Infrastructure`; use the application-layer `ILutraDbContext` from handlers rather than the concrete context.
- When changing the schema, add an EF migration and ensure the migrator still applies it before the API starts. `LutraSeeder` reconciles on every run: it upserts `Albert Heijn`, `Jumbo`, `Poiesz`, and `Lidl` by name and re-applies the seeded Jumbo Satépannetje (scalars, voedingswaarden, ingredienten and allergenen), so changed seed definitions are refreshed.

## Verification

- Add or update unit tests in `Lutra.Application.UnitTests` for handler/business behavior, tests in `Lutra.Infrastructure.OpenRouter.UnitTests` for the extractor/HTML/SSRF helpers (the Application unit-test project does not reference the OpenRouter project), and integration tests in `Lutra.API.IntegrationTests` for HTTP behavior.
- Do not assume integration tests exercise PostgreSQL-specific behavior; they run on SQLite unless a separate database-backed test is explicitly added.
- Keep the build warning-free: after a change, run `dotnet build Lutra.sln` and resolve every warning it reports (compiler, analyzer, or MSBuild), including pre-existing warnings in projects you touch. Only leave a warning in place when no supported fix exists; in that case suppress it narrowly (for example `NoWarn` for the specific code) and explain why in the changelog instead of leaving it unfixed.
- A GitHub Actions workflow (`.github/workflows/build-and-test.yml`) builds the solution and runs the unit and integration tests on pushes to `main`; locally, `dotnet build` plus running the unit and integration test projects (see Commands) are the available baseline checks.

## Naming and Style

- Use English for general code names and Dutch only for domain-specific enums, types, folders, and files such as `Verspakketten`, `Supermarkt`, and `Beoordeling`.
- Keep class and file names consistent with the feature name. Use sealed types where the existing codebase does so.
- Prefer explicit `required` members where the current codebase uses them.
- Use file-scoped namespaces (`namespace Foo.Bar;`), never block-scoped namespaces.
- Preserve the indentation and brace style of the file being changed.

## Testing Guidance

- Use xUnit with FluentAssertions for new tests; use the existing Moq-based unit-test patterns where applicable.
- Keep tests readable and focused on behavior. Update or add tests alongside behavior changes.
- Integration tests use `LutraApiFactory` and `IntegrationTestBase` as their base infrastructure.

## Change Workflow

- Read the relevant file before editing.
- Make the smallest change that solves the problem and reuse existing repository patterns instead of inventing new ones.
- When the change affects the API contract (endpoints, request/response bodies, enums, limits, error mapping, or background-command behavior), update `docs/api.md` in the same change with the new or adapted worked examples and keep the reference tables in sync; mention the documentation update in the changelog entry.
- Run or request build/test verification after changes when appropriate; the build must finish with zero warnings (see Verification), and avoid broad refactors unless explicitly requested.


## Changelog

- After completing each user prompt, add an entry to `Changelog.md` in the solution root summarizing the change in 1 to 5 sentences.
- Each entry is a dash-prefixed bullet placed under a header for the current date, formatted like `## 22 July 2026`.
- If an entry already exists for today's date, append the new bullet under the existing header instead of creating a new one.
- Keep entries factual and concise; do not restate implementation details already obvious from the diff.

## Available agents

Specialist agent definitions live under `Lutra/.github/agents/` (one `<name>.agent.md` per agent). Consult the matching agent when the task fits its description.

## Specialist Agents

- Use `csharp-expert.agent.md` for C# quality, `dotnet-expert.agent.md` for .NET architecture, `security-reviewer.agent.md` for API security, `debug.agent.md` for systematic debugging, `postgresql-dba.agent.md` for PostgreSQL and migrations, and `test-generator.agent.md` for tests.
