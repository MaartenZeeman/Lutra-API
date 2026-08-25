# Lutra Repository Instructions

## Working Directory

- The solution and all .NET projects are under `Lutra/`; run `dotnet` and Docker commands from that directory unless a command says otherwise.

## Structure

- This is a .NET 10 Clean Architecture solution: `Lutra.Domain` has no project dependencies, `Lutra.Application` depends on Domain, `Lutra.Infrastructure` implements persistence for Application, and `Lutra.API` depends on Application and Infrastructure.
- Application use cases use `Cortex.Mediator` and are organized as feature partials (`FeatureName.Query/Command`, `Handler`, `Response`); keep controllers thin and delegate to the mediator.
- `Lutra.AppHost` is Aspire orchestration only. `Lutra.Infrastructure.Migrator` is a one-shot host that applies EF migrations and seeds supermarkets.

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

## Aspire and Migration Rules

- Keep `Lutra.AppHost` responsible for orchestration only and preserve the persistent PostgreSQL container unless the change explicitly requires otherwise.
- Keep the migrator as a one-shot project that applies migrations on startup. Ensure migration changes do not break runtime assembly/version consistency.

## Commands

Run from `Lutra/`:

```bash
dotnet restore Lutra.sln
dotnet build Lutra.sln
dotnet test Lutra.sln
dotnet test Lutra.Application.UnitTests/Lutra.Application.UnitTests.csproj
dotnet test Lutra.API.IntegrationTests/Lutra.API.IntegrationTests.csproj
dotnet test Lutra.API.IntegrationTests/Lutra.API.IntegrationTests.csproj --filter "FullyQualifiedName~VerspakkettenControllerTests"
dotnet run --project Lutra.AppHost/Lutra.AppHost.csproj
```

- Integration tests do not require PostgreSQL: `LutraApiFactory` replaces the API's Npgsql registration with one shared open SQLite in-memory connection and creates the schema with `EnsureCreated`; tests reset data after each test through `IntegrationTestBase`.
- Running the API directly uses PostgreSQL and requires `ConnectionStrings:LutraDb` (or environment variable `ConnectionStrings__LutraDb`); the Development settings contain a placeholder password.
- Aspire provisions a persistent PostgreSQL container, creates `LutraDb`, waits for the migrator to finish, then starts the API. Docker must be available for the AppHost flow.

## Persistence Changes

- EF Core implementation and migrations belong in `Lutra.Infrastructure`; use the application-layer `ILutraDbContext` from handlers rather than the concrete context.
- When changing the schema, add an EF migration and ensure the migrator still applies it before the API starts. The migrator seeds `Albert Heijn`, `Jumbo`, `Poiesz`, and `Lidl` only when the supermarket table is empty.

## Verification

- Add or update unit tests in `Lutra.Application.UnitTests` for handler/business behavior and integration tests in `Lutra.API.IntegrationTests` for HTTP behavior.
- Do not assume integration tests exercise PostgreSQL-specific behavior; they run on SQLite unless a separate database-backed test is explicitly added.
- No CI workflows, formatter, linter, or pre-commit configuration is present in the repository; `dotnet build` and `dotnet test` are the available baseline checks.

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
- Run or request build/test verification after changes when appropriate; avoid broad refactors unless explicitly requested.


## Changelog

- After completing each user prompt, add an entry to `Changelog.md` in the solution root summarizing the change in 1 to 5 sentences.
- Each entry is a dash-prefixed bullet placed under a header for the current date, formatted like `## 22 July 2026`.
- If an entry already exists for today's date, append the new bullet under the existing header instead of creating a new one.
- Keep entries factual and concise; do not restate implementation details already obvious from the diff.

## Available agents

opencode specialist agents live in `.opencode/agents/` (one `<name>.md` per agent). Create or invoke them there when the task matches their description.

## Specialist Agents

- Specialist guidance is available under `Lutra/.github/agents/`: use `csharp-expert.agent.md` for C# quality, `dotnet-expert.agent.md` for .NET architecture, `security-reviewer.agent.md` for API security, `debug.agent.md` for systematic debugging, `postgresql-dba.agent.md` for PostgreSQL and migrations, and `test-generator.agent.md` for tests.
