# CLAUDE.md

Guidance for AI assistants working in the SeatRush repository.

## Project context

SeatRush is an event ticketing system and a **learning lab for system design in .NET on Azure**. The owner (Mohamad) makes the architectural decisions and reviews every change; you implement them.

Because this is a learning project, some patterns are used **deliberately** even where a simpler solution would work. When you apply a pattern, explain briefly **why it's used here and when it wouldn't be**.

Read before starting any task:
- `README.md`: goals and architecture
- `docs/roadmap.md`: the phase plan, the current step, and decisions already made. **Find where the task fits before starting.** Don't build things planned for a later phase. Update the step's status in the PR that finishes it.
- `docs/adr/`: architecture decisions (these are binding)
- `docs/coding-guidelines.md`: how code should be written (access modifiers, SOLID, domain modeling)

**Teaching.** Mohamad decides; you explain. Present open decisions **one at a time**: options, trade-offs, a recommendation, and when the recommendation would be wrong. When code applies a principle from the coding guidelines in a non-obvious way, name it in the PR or a short comment.

**Project files are the source of truth, not AI memory.** Everything needed to continue work lives in the repo: plan and progress in `docs/roadmap.md`, decisions in `docs/adr/`, rules here and in `docs/coding-guidelines.md`. When a step finishes, a decision is made, or work stops halfway, record it in those files, not in an assistant's private memory or notes.

## Tech stack

- .NET 10, ASP.NET Core **controllers**
- EF Core with SQL Server (local: Docker via .NET Aspire; cloud: Azure SQL)
- Redis, Azure Service Bus, Azure Functions, Blob Storage (added per phase)
- xUnit + Testcontainers
- GitHub Actions, Docker, Bicep (planned for Phase 6), k6 (planned for Phase 5)

## Architecture

**Modular monolith.** Modules: `Identity`, `Events`, `Booking`, `Payments`, `Notifications` (see ADR 0001).

- Each module has its own `Domain`, `Application`, and `Infrastructure` layers.
- **Modules never reference each other directly.** They communicate only through public contracts or integration events.
- `Shared` contains common abstractions only. No business logic.
- No new module, cross-module dependency, or major library **without an ADR**. If one seems needed, stop and propose it.

**CQRS-lite.** Commands (writes) and queries (reads) are separate, with **one handler per use case**, organized by feature (vertical slices), e.g. `Booking/Features/HoldSeat/`. Same database for reads and writes. No MediatR; use the project's own handler abstractions.

**Domain model.**
- `Booking` and `Payments` use a **rich domain model**: business rules live in entities and value objects, with private setters, factory methods, and domain events.
- `Events` and other simple areas may use straightforward CRUD.
- Controllers are thin: validate → dispatch to a handler → map the result. No business logic in controllers.

## Coding conventions

- Async all the way. Pass `CancellationToken` everywhere. Never use `.Result` or `.Wait()`.
- Use `AsNoTracking()` for read-only queries.
- Errors: domain errors via the Result pattern; return RFC 7807 problem details from the API.
- Validation with FluentValidation at the edge; invariants enforced in the domain.
- Use only patterns planned for the current phase. Add a short comment or PR note on **why / when not**.
- Prefer clear, explicit code over clever code.
- Write senior-level code, not just code that works: follow `docs/coding-guidelines.md` and check a change against its review list before opening a PR.

## Database

- **One DbContext and one schema per module.**
- **Never edit a migration that has been applied.** Always add a new migration.
- Name migrations descriptively (e.g. `AddSeatHoldExpiry`).

## Testing

- **Every feature ships with tests:**
  - Unit tests for domain logic and handlers
  - Integration tests with Testcontainers for persistence and API endpoints
- Architecture tests enforce module boundaries; never weaken them to make code compile.
- All tests must pass before a PR is opened.

## Security

- **No secrets in code or committed config files.** Use user-secrets locally and Azure Key Vault in the cloud.
- Never log personal or payment data.

## Workflow

- GitHub Flow: `main` is the only long-lived branch. One feature per branch (`feature/<short-name>`, or `docs/<short-name>` for documentation only), small focused PRs.
- **Do not touch files outside the scope of the task.**
- **Ask before guessing.** If a requirement is unclear, ask; never invent business rules.
- PR description must include:
  - **What** changed
  - **Why**
  - **Trade-offs** and alternatives considered
  - **How to test**
  - **Review findings**: what the checks below found and how each was handled (omit if none ran or nothing was found)
  - **Learning notes**: patterns and principles applied, and when they wouldn't fit (omit for trivial or docs-only PRs)

### Before opening a PR

1. `dotnet build SeatRush.slnx` and `dotnet test --solution SeatRush.slnx` are green. **Always.**
2. Self-check the change against the review list in `docs/coding-guidelines.md`. *Code changes.*
3. Run `/code-review medium` (bugs and edge cases; without `--fix`). Fix the findings and explain each one. *Code changes.*
4. Run `/security-review`. *Only for security-sensitive changes: Identity, Payments, secrets/configuration, deploy workflows.*

Skip steps 2–4 for docs-only and trivial changes (a rename, a version bump). These checks make PRs better; Mohamad's review is the final gate.

## Commands

Prerequisites: .NET SDK 10.0.400+ (pinned in `global.json`), Docker Desktop running (for Aspire).

```bash
# Build everything (warnings are errors)
dotnet build SeatRush.slnx

# Run all tests (uses Microsoft.Testing.Platform, configured in global.json)
dotnet test --solution SeatRush.slnx

# Run locally with Aspire: starts SQL Server + Redis containers, the Api, and the dashboard
dotnet run --project aspire/SeatRush.AppHost

# Run only the Api, without Aspire or containers (health: http://localhost:5080/health)
dotnet run --project src/Api/SeatRush.Api
```

*Add migration: to be added when the first module DbContext exists.*

Notes:
- Package versions live only in `Directory.Packages.props`; `.csproj` files use `<PackageReference Include="..." />` without a version.
- The `Aspire.AppHost.Sdk` version is pinned in `global.json` (`msbuild-sdks`), not in the `.csproj`.
- On Windows, Smart App Control blocks freshly built DLLs ("An Application Control policy has blocked this file"). It must be off on development machines.
