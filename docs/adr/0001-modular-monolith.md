# ADR 0001: Use a modular monolith architecture

- **Status:** Accepted
- **Date:** 2026-10-01
- **Decision owner:** Mohamad

## Context

SeatRush is an event ticketing system where users browse events, hold seats, and purchase tickets. Its core challenge is handling sudden traffic spikes when popular events go on sale, with thousands of users competing for a limited number of seats at the same time. The system must guarantee that a seat is never sold twice, that no paid order is lost, and that it degrades gracefully under overload instead of crashing.

SeatRush is built by a single developer acting as architect, decision-maker, and code reviewer, with AI assistants writing most of the implementation. All deployment, monitoring, and incident handling is done by that one person. The architecture must therefore keep operational overhead low, stay easy to debug end-to-end, and make it simple to review every change, while still enforcing clear boundaries so the codebase stays maintainable as AI generates more code.

As a personal learning project, SeatRush runs on a minimal budget, targeting under $20/month by relying on Azure free tiers and scale-to-zero services. Each additional deployable unit adds its own hosting, database, pipeline, and monitoring costs, so the number of separately deployed services must stay as small as possible.

SeatRush is primarily a learning project for system design. The architecture should let the developer practice clear module boundaries, communication through events, and domain modeling from day one, and later experience distributed-system concerns by extracting a module into a separate service, without paying the cost of microservices before they are needed.

## Decision

SeatRush will be built as a **modular monolith**: a single deployable application split into five modules:

| Module | Owns |
|---|---|
| **Identity** | User accounts, authentication (ASP.NET Core Identity + JWT), and roles: Customer, Organizer, Admin |
| **Events** | Events, venues, seating layouts, ticket pricing, and sale schedules |
| **Booking** | Seat availability per event, seat holds, reservations, and the order lifecycle |
| **Payments** | Payment processing, provider integration, and refunds |
| **Notifications** | Emails and ticket delivery (QR codes); reacts to other modules' events and makes no business decisions |

Rules:

- Each module owns its data in a **separate database schema** and exposes only a **public contract**.
- Modules communicate through **integration events** for side effects, and through **read-only public contracts** (e.g. `IEventsModuleApi`) for synchronous queries.
- Other modules reference users only by ID (`CustomerId`, `OrganizerId`). Identity handles authentication and roles; each module enforces its own resource rules.
- Direct references to another module's internals or database are **forbidden** and enforced by **architecture tests**.
- Integration events start **in-process** and move to **Azure Service Bus with an outbox** in a later phase.

## Alternatives considered

**Traditional layered monolith:** Rejected. Organizing by technical layers lets any service call any other service and query any table. As the codebase grows, especially with AI generating code that tends to take the shortest path, dependencies tangle, changes ripple across features, and extracting a part into a separate service becomes a major rewrite. A modular monolith keeps the same simple deployment while enforcing boundaries that make extraction straightforward.

**Microservices from day one:** Rejected. Microservices mainly solve organizational problems (many teams deploying independently) and allow scaling individual parts separately. With a single developer, these benefits don't apply, while the costs do: separate deployments, pipelines, and databases per service; network failures between services; distributed transactions; and much harder debugging. The monolith can still scale horizontally by running multiple instances. A module will be extracted only when a concrete need appears.

## Consequences

### Positive

- Single deployment: one pipeline, one app, fast and simple releases.
- Low cost: one hosting unit instead of one per service.
- Easier debugging: one process, end-to-end traces, no network hops between modules.
- Simpler consistency: in-process calls avoid network failures and distributed transactions.
- Enforced boundaries keep the codebase maintainable as it grows.
- Modules can be extracted into services later with minimal changes, since callers depend only on contracts and events.

### Negative

- **Single point of failure:** a crash, memory leak, or thread pool starvation in one module affects all modules.
- **Scales as a unit:** Booking can't be scaled alone during a ticket drop; the whole app scales together.
- **Shared deployment:** a small change in one module requires redeploying everything.
- **More ceremony than a plain monolith:** contracts, integration events, and architecture tests add code and upfront effort.
- **Shared resources:** modules compete for the same CPU, memory, and database server.
- **Eventual consistency between modules:** communication through events means data across modules isn't instantly in sync, even inside one app.

### When to revisit

- A module's load requires scaling it independently, e.g. Booking under ticket drops while the rest of the app sits idle.
- Failures in one module repeatedly affect others despite resilience measures.
- A module needs a different deployment cadence or technology.
- As a planned learning step, **Notifications** will be extracted into a separate service. It's asynchronous and independent, making it the lowest-risk candidate.
