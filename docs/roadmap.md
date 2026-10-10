# SeatRush roadmap

The plan for building SeatRush, phase by phase. It is a living document: it changes through PRs like any other file.

## How this roadmap works

- **Every phase has a goal and a scope.** Only the **current phase** is broken down into concrete steps (one step ≈ one or a few PRs). Later phases are detailed when they start, because what we learn in earlier phases shapes them (*rolling-wave planning*).
- **Each phase answers one system design question.** Every phase ends with at least one experiment write-up backed by tests or measurements.
- **Decisions marked _planned_** were agreed while drafting this roadmap. They are confirmed (or changed) by an ADR in `docs/adr/` when the phase starts. ADRs are binding; this file is the plan.
- **Status:** ✅ done · 🔄 in progress · ⬜ not started. Update the status in the same PR that finishes a step.

### Workflow for every step

1. Read this file, the ADRs, and `CLAUDE.md`
2. Discuss open decisions one at a time (options, trade-offs, recommendation); record major ones as an ADR
3. Branch `feature/<short-name>` from `main`
4. Implement with tests; build and tests green locally
5. PR with What / Why / Trade-offs / How to test; CI green; Mohamad reviews and merges
6. Update the status here

## Overview

| Phase | System design question | Modules | Runs on | Status |
|---|---|---|---|---|
| 1. Foundations | How do you structure a system so it can grow, and check every change? | Events | Local | 🔄 |
| 2. Booking under pressure | How do you guarantee exactly one owner per seat under heavy concurrency? | Identity, Booking | Local | ⬜ |
| 3. Payments & reliability | How do you take money safely when things fail halfway? | Payments | Local | ⬜ |
| 4. Async & cloud | What can happen later, and how do you make background work reliable? | Notifications | **Azure (first deploy)** | ⬜ |
| 5. Scale & observe | Where does the system break during an on-sale rush, and how do you see it? | — | Azure | ⬜ |
| 6. Production-ready | What makes it trustworthy with real customers? | — | Azure | ⬜ |

**Why Azure only from Phase 4:** Phases 1–3 need nothing that Docker and Aspire can't run locally, and cloud costs and trial credits are limited. Phase 4 needs real Azure services anyway (Service Bus, Functions, Blob Storage). Until then the code stays **deploy-ready**: no secrets in code, settings through configuration, and CI that builds everything. *Fallback if cost becomes a problem: build Notifications against local emulators and deploy at the start of Phase 5.*

---

## Phase 1: Foundations 🔄

**Question:** How do you structure a system so it can grow without turning into a tangled mess, and check every change automatically?

**Goal:** One simple module working end to end (HTTP → handler → database), with automated tests and CI. Everything runs locally.

| # | Step | Learn | Status |
|---|---|---|---|
| 1.1 | Solution skeleton: modules, Shared abstractions, Aspire, architecture tests (PR #3) | Module boundaries, enforcing architecture with tests | ✅ |
| 1.2 | Verify the Aspire AppHost run (SQL Server + Redis containers healthy) | Local orchestration with Aspire | ✅ |
| 1.3 | CI: `.github/workflows/ci.yml` runs build + tests on every PR; then branch protection on `main` | Continuous integration, automated quality gates | 🔄 |
| 1.4 | Events module (several PRs; breakdown decided when the step starts) | Vertical slices, CQRS-lite, EF Core with one schema per module, migrations, FluentValidation, problem details, Testcontainers | ⬜ |

**Decided**
- Phase 1 is **local only**. CD to Azure moved to Phase 4.
- **Events before Identity:** Events is plain CRUD, the simplest module to learn the full slice pattern on. Making Events endpoints admin-only later is cheap (`[Authorize]`). Identity must come before **Booking**, where the user is part of the business logic.
- CI and CD are **separate workflows** (`ci.yml` now, `deploy.yml` in Phase 4): different triggers, different permissions, and no cloud credentials in the PR workflow.
- **CI triggers:** `pull_request` and `push` to `main`. The run on `main` catches two PRs that pass alone but break once both are merged.
- **CI runs on `ubuntu-latest` only:** Testcontainers needs Linux containers (Windows runners can't run them), and production runs Linux containers. Windows is covered by local development.
- **`main` ruleset (`protect-main`):** PR required with 0 approvals (solo project; GitHub doesn't allow approving your own PR), CI job `build-and-test` must pass, no force pushes or deletion, no bypass (applies to admins too). "Branch must be up to date" is off: the `push`-to-`main` run covers it.

**Open at step 1.4 start**
- What an Event contains (venue, date, seat layout, statuses). These are business rules, so Mohamad decides them.
- **Local database persistence:** today the AppHost uses Aspire's defaults, so every run starts with an empty database on a random port. Recommendation: keep SQL Server in Docker (same version everywhere; Docker is needed anyway for Testcontainers and Redis) and add `.WithDataVolume()` + `.WithLifetime(ContainerLifetime.Persistent)` so data survives restarts and the connection details stay stable (e.g. for Rider's database tools). A local SQL Server install was considered and rejected.
- **How migrations are applied locally:** automatically at Api startup in Development, by a separate migration service in Aspire, or by hand with `dotnet ef database update`.
- **Seed data:** sample data created from code, so a fresh or reset database is usable in seconds.

**Out of scope:** seat holds, concurrency, Redis usage, payments, Azure.

---

## Phase 2: Booking under pressure ⬜

**Question:** How do you guarantee one owner per seat when thousands of people click at the same second, and free seats when someone abandons checkout?

**Goal:** Users log in, hold a seat temporarily, and confirm it. Tests and experiments **prove** that no seat is ever double-booked.

**Scope**
- Identity (approach decided by ADR at phase start: ASP.NET Core Identity vs an external provider)
- Seat hold with expiry (first rich domain model: entities with rules, domain events)
- Releasing expired holds (background processing, time as a business rule)
- **Locking strategies compared:** pessimistic locks, optimistic concurrency, unique constraints, Redis atomic operations / distributed locks
- Redis for holds (TTL, trade-offs vs SQL)
- Experiment write-up: the strategies under concurrency, with numbers

**Decided**
- **Booking ends with Hold → Confirm, no payment yet.** This is the smallest scope that proves *final* ownership end to end. Phase 3 inserts payment between hold and confirm without redesigning it.

**Out of scope:** real payments, waiting room, large-scale load tests.

---

## Phase 3: Payments & reliability ⬜

**Question:** How do you take money safely when networks fail, requests get retried, and servers crash halfway through?

**Goal:** The flow becomes **Hold → Pay → Confirm**. Every failure ends in a correct state: either paid and confirmed, or not charged and seat released. Failure-injection experiments prove it.

**Scope**
- Payments module (rich domain model: payment states and transitions)
- First communication between modules through integration events
- Idempotency keys (no double charge on retries or double clicks)
- Outbox pattern (atomically save data and publish events; in-process transport for now)
- Saga / process manager with compensation (release seat, refund)
- Polly: retries, timeouts, circuit breaker
- Experiment write-up: crashes, timeouts, and duplicates injected on purpose

**Decided (planned)**
- **Simulated payment provider** behind `IPaymentGateway`, with configurable failures (timeouts, lost replies, duplicate confirmations). Real providers can't fail on demand, and failure is what this phase is about. Stripe test mode may be added later as a second implementation.

**Out of scope:** Service Bus (Phase 4 replaces the in-process transport without changing business code).

---

## Phase 4: Async & cloud ⬜

**Question:** Which work must happen inside the user's request, and which can happen later? How do you make background work reliable when messages arrive twice, late, or never?

**Goal:** SeatRush runs on Azure for the first time. After a confirmed booking, the user gets a QR ticket and an email, processed asynchronously.

**Scope**
- Azure account with **budget alerts first**
- First deploy: Container Apps, Azure SQL, Redis, Key Vault, managed identity
- `deploy.yml`: CD from `main` after tests pass, Azure login through OIDC (no stored secrets)
- Service Bus replaces the in-process outbox transport
- Notifications module + Azure Functions (QR tickets, emails)
- Blob Storage (event images, ticket files)
- Local emulators (Service Bus emulator, Azurite)
- Experiment write-up: duplicate delivery, poison messages, dead-letter queues, idempotent consumers

**Decided (planned)**
- **Provisioning with Aspire + Azure Developer CLI (`azd`)**: generates Bicep from the AppHost, so deploying is fast and repeatable. Replaced by hand-written IaC in Phase 6.

**Open at phase start (ADRs):** where Notifications runs (inside the monolith or in Functions), email provider.

---

## Phase 5: Scale & observe ⬜

**Question:** What happens when a popular event goes on sale? Where does the system break first, how do you protect it, and how do you see what's happening?

**Goal:** Simulate an on-sale rush on Azure, find bottlenecks by measuring, fix them, and prove each fix with before/after numbers.

**Scope**
- Observability: OpenTelemetry → Application Insights, custom metrics, alerts (local traces already exist in the Aspire dashboard since Phase 1)
- Load-test baseline (throughput, p95/p99 latency, error rates)
- Caching event listings (cache-aside with Redis, invalidation, cache stampede)
- Rate limiting
- Waiting room / virtual queue (admission control)
- Container Apps autoscaling
- Chaos testing (Redis down, slow database)
- Experiment write-ups for each improvement

**Decided (planned)**
- **k6** for load testing: industry standard, free, tests the system as a black box from outside.

**Cost note:** keep cloud load tests short and scale down afterwards.

---

## Phase 6: Production-ready ⬜

**Question:** What separates "it runs in the cloud" from "you could trust it with real customers"?

**Goal:** The environment can be rebuilt from code, deploys cause zero downtime, security has been reviewed, restores have been tested, and costs are understood.

**Scope**
- Hand-written IaC replaces the azd-generated files
- Staging and production environments, with approval before production
- Zero-downtime deploys (Container Apps revisions, blue-green / canary, health probes)
- Backward-compatible migrations (expand/contract)
- Security hardening: OWASP Top 10 review, Dependabot, secret scanning, least-privilege RBAC, private networking
- Backup and disaster recovery, with a tested restore
- Cost tuning and a cost write-up

**Decided (planned)**
- **Bicep**: Azure-native, no state file to manage, and builds on the Bicep seen in Phase 4. Confirmed by ADR at phase start. Terraform would be the choice if skills that carry across clouds became the priority.
