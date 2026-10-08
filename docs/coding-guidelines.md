# Coding guidelines

How code in SeatRush should be written: not just code that works, but code that is clear, safe to change, and reads like it was written by a senior developer on a professional team.

These guidelines are also a **learning tool**. When code applies one of them in a non-obvious way, the PR or a short comment names the principle and says why it was applied. That way every PR teaches something.

Architecture rules (modules, layers, CQRS-lite) live in `CLAUDE.md` and `docs/adr/`. This file is about the code inside those boundaries.

## 1. Access modifiers: as closed as possible

Start with the most restrictive option and open up only when there's a reason.

| Element | Default | Open it up when… |
|---|---|---|
| Classes | `sealed`; `internal` if only its own project uses it | `public`: another project uses it. In SeatRush that's common, because each layer is its own project (Domain types are used by Application, Application handlers by the Api). Implementation details stay `internal` (EF Core configurations, repository implementations, helpers). Not sealed: designed for inheritance, e.g. `Entity<TId>`. |
| Fields | `private readonly` | Almost never. Expose data through properties or methods. |
| Property setters | `private set` or `init` | Never a public `set` on a domain entity. State changes go through methods that enforce the rules. |
| Methods | `private` | `public` only when it's part of what the type is *for* |
| Constructors of domain entities | `private` | Use a static factory method (`Booking.Create(...)`) that validates and returns a `Result` |

**Why:** everything `public` is a promise other code can depend on, and promises are expensive to change. `internal` hides implementation details, which the compiler enforces for free. (Other *modules* are kept out by the architecture tests, not by `internal`: they may only reference `Contracts`.) `sealed` says "not designed for inheritance" and lets the compiler optimize. Opening something later is easy; closing it once others use it is not.

## 2. SOLID, in practice

| Principle | What it means here |
|---|---|
| **S**ingle responsibility | One reason to change. One handler per use case (`HoldSeatHandler`, not `BookingService` with 20 methods). |
| **O**pen/closed | Add behavior by adding new code, not by editing working code. A new payment gateway is a new `IPaymentGateway` implementation, not another `if` branch. |
| **L**iskov substitution | Any implementation of an interface must be usable where the interface is expected without surprises: no `NotImplementedException`, no stricter preconditions. |
| **I**nterface segregation | Small, focused interfaces. A handler that only reads shouldn't depend on an interface that can also delete. |
| **D**ependency inversion | Domain and Application define the interfaces they need. Infrastructure implements them. Dependencies point inward, which the architecture tests enforce. |

**When not to:** SOLID is a tool, not a goal. An interface with exactly one implementation and no test seam or boundary reason is just noise. Don't add abstractions "in case" (see YAGNI).

## 3. Other principles

- **KISS:** the simplest solution that meets the requirement. Clever code costs more to review than it saves.
- **YAGNI:** don't build for requirements that don't exist yet. The roadmap tells us what's coming; build it when its phase arrives.
- **DRY, with care:** remove duplicated *knowledge* (a business rule in two places), not duplicated-looking code. Wait for the third occurrence before extracting (*rule of three*). The wrong abstraction is worse than duplication.
- **Tell, don't ask:** `booking.Confirm()`, not `if (booking.Status == Held) booking.Status = Confirmed`. The object that owns the data owns the rules.
- **Law of Demeter:** talk to direct collaborators only. `order.Customer.Address.City` from outside is a sign that a method is missing.
- **Composition over inheritance:** inherit only for true "is-a" relationships with shared behavior (`Entity<TId>`). Otherwise inject or compose.
- **Fail fast:** validate at the edge (FluentValidation) and guard invariants in the domain. An invalid object should be impossible to create.

## 4. Domain modeling (Booking, Payments)

- **Make invalid states unrepresentable.** Value objects for concepts with rules (`SeatNumber`, `Money`), not raw `string`/`decimal` (*primitive obsession*).
- Value objects are **immutable** (`sealed record` or readonly struct) and validate on creation.
- Entities change state only through **methods named after business actions** (`Hold`, `Confirm`, `Release`), which enforce the rules and raise domain events.
- Expected business failures return a **`Result`**. Exceptions are for truly unexpected situations (bugs, infrastructure failures), never for control flow.
- Collections are exposed read-only (`IReadOnlyList<T>`), backed by a private list.

## 5. Everyday C#

- **Nullable reference types are on.** Treat every warning as a real bug. Avoid `!` (null-forgiving) unless there's a comment explaining why it's safe.
- **Immutability by default:** `readonly` fields, `init` properties, `record` for data carriers (commands, queries, DTOs, events).
- **Guard clauses** instead of deep nesting: handle the bad cases first and return early.
- **Async correctly:** `async` all the way, pass `CancellationToken`, never `.Result`/`.Wait()`, no `async void` (except event handlers).
- **Names say intent:** `GetAvailableSeatsAsync`, not `GetData`. Booleans read as questions (`IsExpired`, `CanBeConfirmed`). No abbreviations except well-known ones (`Id`, `Dto`).
- **Small methods, one level of abstraction each.** If a method needs comments to separate its parts, the parts are probably methods.
- **Comments explain *why*, not *what*.** The code says what. Comments cover decisions, trade-offs and non-obvious constraints.
- **No magic values:** named constants or configuration (`HoldDuration`, not `TimeSpan.FromMinutes(10)` repeated in three places).
- **Dependencies through the constructor.** No service locator, no static mutable state. Use primary constructors for simple injection.
- **Time is a dependency:** use `TimeProvider`, not `DateTime.UtcNow`, so time-based rules (hold expiry) are testable. Store times in UTC.

## 6. Tests

- Test names describe behavior: `Hold_fails_when_seat_is_already_held`.
- One behavior per test. Arrange / Act / Assert, visibly separated.
- Test through public behavior, not private details. Tests that break on every refactor are testing the wrong thing.
- Unit tests for domain rules and handlers; integration tests (Testcontainers) for persistence and API endpoints.

## 7. Reviewing against these guidelines

Before opening a PR, check the code for:
- Anything `public` that could be `internal`, or a class that could be `sealed`?
- Public setters on domain types?
- Business rules outside the domain (in controllers, handlers, or infrastructure)?
- Abstractions without a current reason?
- Names a new team member would misunderstand?
