# Copilot Instructions for Lutra

Use these standards when generating or modifying code in this repository.

## Project overview

- Target framework: .NET 10 (`net10.0`)
- Architecture: Clean Architecture
- Solution layers:
  - `Lutra.Domain`
  - `Lutra.Application`
  - `Lutra.Infrastructure.Sql`
  - `Lutra.API`
  - `Lutra.AppHost`
  - `Lutra.Infrastructure.Migrator`
- Database: PostgreSQL via EF Core
- Orchestration: .NET Aspire
- Messaging pattern: `Cortex.Mediator` for CQRS handling

## General standards

- Prefer small, focused changes.
- Follow existing naming, formatting, and folder conventions.
- Keep dependencies pointing inward:
  - Domain depends on nothing
  - Application depends on Domain
  - Infrastructure depends on Application and Domain
  - API depends on Application and Infrastructure
  - AppHost and Migrator are host projects only
- Use nullable reference types correctly and keep implicit usings compatible with the project style.

## Clean Architecture guidance

Use Jason Taylor’s CleanArchitecture project as a reference for intent and structure, but adapt to this codebase’s actual implementation.

Where the reference template uses MediatR, this project uses `Cortex.Mediator`.
Add FluentValidation, AutoMapper, NUnit, Shouldly, Moq, or Respawn only if the repository already uses them or the change clearly requires them.

## Domain layer rules

- Keep entities simple and focused on business state.
- Use `BaseEntity` as the shared base type when appropriate.
- Preserve the current entity style:
  - `Id` as `Guid`
  - audit fields like `CreatedAt`, `ModifiedAt`, `DeletedAt`
  - `IsDeleted` as a derived property
- Keep validation and persistence concerns out of Domain unless they are intrinsic business rules.

## Application layer rules

- Put use cases in feature folders.
- Follow the existing CQRS split:
  - `FeatureName.cs` as the partial entry point
  - `FeatureName.Query.cs` or `FeatureName.Command.cs`
  - `FeatureName.Handler.cs`
  - `FeatureName.Response.cs`
- Use `Cortex.Mediator` request and handler interfaces.
- Keep handlers focused on orchestration and application logic.
- Use `ILutraDbContext` rather than referencing the concrete DbContext directly when possible.

## Infrastructure rules

- Put EF Core implementation, migrations, and database wiring in `Lutra.Infrastructure.Sql`.
- Keep the DbContext implementation aligned with `ILutraDbContext`.
- If package versions must be pinned for runtime compatibility, pin them explicitly in the infrastructure project.
- Be cautious with `PrivateAssets="all"` on design-time packages because they do not flow dependency constraints to downstream projects.

## API layer rules

- Keep controllers thin.
- Controllers should delegate to Application use cases through `IMediator`.
- Keep `Program.cs` focused on DI and middleware setup.

## Aspire and migration rules

- Keep `Lutra.AppHost` responsible for orchestration only.
- Preserve the persistent PostgreSQL container setup unless a change explicitly requires otherwise.
- Keep the migrator as a one-shot project that applies migrations on startup.
- Ensure migration-related changes do not break runtime assembly/version consistency.

## Naming and code style

- Match the existing language of the codebase; this project uses Dutch domain names in places such as `Verspakketten`, `Supermarkt`, and `Beoordeling`.
- Keep class and file names consistent with the feature name.
- Use sealed types where the project already does.
- Prefer explicit `required` members where the current codebase uses them.
- Preserve current indentation and brace style in each file.

## Testing guidance

- If tests exist, update or add tests alongside behavior changes.
- Follow the reference template’s intent for test coverage:
  - unit tests for business logic
  - integration tests for infrastructure and data access
  - functional tests for API behavior
- Keep tests readable and focused on behavior.

## When making changes

- Read the relevant file before editing.
- Make the smallest change that solves the problem.
- Reuse existing patterns from the repo instead of inventing new ones.
- Run or request a build/test verification after changes when appropriate.
- Avoid broad refactors unless the user asks for them.
