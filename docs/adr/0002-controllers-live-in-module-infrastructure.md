# ADR 0002: Controllers live in each module's Infrastructure layer

- **Status:** Accepted
- **Date:** 2026-10-10
- **Decision owner:** Mohamad

## Context

Step 1.4c adds the first HTTP endpoints (`EventsController`). A controller needs its module's commands, queries and handlers, so its location decides how much of each module the Api project can see.

Until now the Api has been a thin **composition root**: it references each module's Infrastructure project only to call `Add<Module>Module()` (see its `.csproj`). Each module has exactly four layer projects (Contracts, Domain, Application, Infrastructure), enforced by the architecture tests. ADR 0001 aims for modules that are self-contained, so one can later be extracted into its own service with minimal changes.

## Decision

**Each module's controllers live in its Infrastructure project** (e.g. `SeatRush.Events.Infrastructure/Api/`). A separate `Add<Module>Endpoints()` on `IMvcBuilder` registers them with `AddApplicationPart`, so the Api discovers them without referencing their types. It is kept apart from `Add<Module>Module()` (the module's services) so hosts that don't serve HTTP, like the migration service, get the services without the MVC stack; only the Api calls both.

- The controllers are the module's **inbound adapters** (HTTP → handlers), just as repositories are its **outbound adapters** (handlers → database). Infrastructure is the adapters layer.
- Controllers stay thin: validate → dispatch to a handler → map the result (CLAUDE.md).
- The Api keeps cross-cutting HTTP concerns that are the same for every module (problem details, exception handling, health checks) and still references modules only to call their registration methods (`Add<Module>Module()` and `Add<Module>Endpoints()`).

## Alternatives considered

**Controllers in the Api project:** Rejected. It's the most common layout and puts the whole API surface in one place, but the Api would depend on every module's Application types and become the place where all modules meet. Modules would no longer own their full slice, which makes extracting one harder.

**A fifth layer per module (`<Module>.Presentation`):** Rejected for now. It's the cleanest separation, but it adds five projects for a handful of endpoints and changes the "exactly four layer projects" rule. Revisit if a module's HTTP layer grows large.

## Consequences

### Positive

- Each module owns its full slice, from HTTP down to its schema; extracting a module means moving its projects, not untangling the Api.
- The Api stays a thin host, and the existing architecture tests keep working unchanged.

### Negative

- Infrastructure depends on ASP.NET Core and holds both inbound and outbound adapters.
- Endpoints are spread across modules instead of one folder; consistent conventions (routes, problem details) must be kept by shared code or by review. Modules can't reference the Api, so shared HTTP helpers need their own project once a second module has controllers.

### When to revisit

- A module's HTTP layer grows large enough that a separate Presentation project pays for itself.
- The API needs one centrally managed surface (shared versioning, gateway-style routing).
