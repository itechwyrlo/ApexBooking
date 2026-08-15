# Hangfire + Outbox Core: Design

## Problem

Two related gaps exist in ApexBooking's background processing today (full detail in the companion audit):

1. **No working background job.** `BackgroundWorker`/`BackgroundTaskQueue` are registered but nothing ever enqueues to them. `TrialExpiryJob`/`TrialExpiryWorker` are fully commented out.
2. **Transactional emails are not reliable.** The 6 domain-event email handlers (`SendBookingConfirmationEmailHandler`, `SendThankYouEmailOnBookingCompletedHandler`, `SendStaffSetupInvitationOnTeamMemberInvitedHandler`, `SendTenantRequestReceivedEmailHandler`, `SendOwnerSetupInvitationOnTenantCreatedHandler`, `SendTenantRequestRejectedEmailHandler`) run synchronously, inline, **after** `SaveChangesAsync` commits in `UnitOfWork.CompleteAsync` — not in the same transaction as the business write. If the process crashes between commit and dispatch, or the SMTP call throws, the business data is saved but the email is silently lost with no record it should have gone out.

Related docs:
- [`2026-08-07-background-jobs-current-state-audit.md`](2026-08-07-background-jobs-current-state-audit.md) — what exists today
- [`2026-08-07-background-jobs-hangfire-outbox-architecture-reference.md`](2026-08-07-background-jobs-hangfire-outbox-architecture-reference.md) — layer/package boundary map this design must respect

## Scope

This is the **first** of several planned sub-projects in the broader background-jobs refactor. This spec covers only: standing up Hangfire, implementing the transactional outbox pattern, and migrating the 6 existing email handlers onto it.

**Explicitly out of scope for this spec** (separate future specs, tracked in project memory):
- Rebuilding `TrialExpiryJob` as a Hangfire `RecurringJob` — deliberately deferred, do not touch/clean up the existing commented-out code as part of this work.
- Replacing FCM push with a Hangfire job that persists `Notification` + pushes via SignalR to the in-app bell.
- The SuperAdmin failed-job monitoring dashboard (Hangfire `Failed`-state filter → persisted table → manual retry by `JobId`).

`ProvisionTenantOnRequestApprovedHandler` (business logic, not an external call) is explicitly **not** migrated to the outbox — it keeps running synchronously in-request exactly as it does today.

## Architecture

Respects the existing Clean Architecture boundary: `Core.Application` and `Infrastructure` never reference each other, only through interfaces defined in `Core.Domain`. No project gains a new *project* reference as part of this work — only NuGet package additions to `Infrastructure`.

| Piece | Project | New dependency |
|---|---|---|
| `OutboxMessage` entity | `Core.Domain/Entities` | — |
| `IReliableDomainEvent` marker interface | `Core.Domain` | — |
| `IOutboxRelayService` port | `Core.Domain` | — |
| `IOutboxTrigger` port | `Core.Domain` | — |
| `RemoveDomainEvent(event)` method | `SharedKernel` base entity | small addition alongside existing `ClearDomainEvents()` |
| `OutboxRelayService` (implements `IOutboxRelayService`) | `Core.Application` | uses existing `IPublisher` (MediatR) — no new package |
| `OutboxMessage` EF mapping | `Core.Persistence/Mappings` | follows existing `IEntityTypeConfiguration<T>` convention |
| `UnitOfWork.CompleteAsync` changes | `Core.Persistence` | none — still only depends on `Core.Domain` |
| Hangfire Outbox Relay recurring job | `Infrastructure/BackgroundJobs/` (next to `TrialExpiryJob`) | depends only on `IOutboxRelayService` (port) |
| `HangfireOutboxTrigger` (implements `IOutboxTrigger`) | `Infrastructure` | `Hangfire.Core` package |
| Hangfire dashboard auth filter | `Infrastructure` | `Hangfire.AspNetCore` package |
| Hangfire server/storage wiring | `Infrastructure` (`AddInfrastructureService`) | `Hangfire.AspNetCore`, `Hangfire.SqlServer` packages |
| Dashboard mount point | `WebApi/Program.cs` | one extension method call, e.g. `app.UseInfrastructureHangfireDashboard()` |

The 6 existing email domain events (`BookingScheduledDomainEvent`, `BookingCompletedDomainEvent`, `TeamMemberInvitedDomainEvent`, `TenantRequestReceivedDomainEvent`, `TenantCreatedDomainEvent`, `TenantRequestRejectedDomainEvent`) start implementing `IReliableDomainEvent`. The 6 handler classes themselves are unchanged — they still run via MediatR, just triggered by the relay instead of the original inline dispatch.

## `OutboxMessage`

Not tenant-scoped — does **not** implement `ITenantEntity`. If it did, `ApexBookingDbContext`'s automatic global query filter would scope every query to whatever tenant happens to be ambient, but the relay job runs with no ambient tenant and must see pending rows across every tenant (the same "no ambient tenant in background contexts" gotcha already on file for the email handlers). Any `TenantId` an event carries is stored as plain row data for traceability, not as a filtered column.

Columns:
- `Id` (Guid, PK)
- `EventType` (string — resolves back to the concrete domain event type on read)
- `Payload` (string — JSON-serialized event)
- `OccurredAtUtc`
- `Status` (`Pending` / `Processing` / `Processed` / `Failed`) — `Processing` is the transient claim state described under "`IOutboxRelayService` mechanics" below
- `ProcessedAtUtc` (nullable)
- `RetryCount` (int)
- `LastError` (string, nullable)

## Write path — same-transaction atomicity

`UnitOfWork.CompleteAsync` gains a step **before** the existing `SaveChangesAsync()` call: it walks the same tracked-entities-with-domain-events list it already builds today, filters to events implementing `IReliableDomainEvent`, serializes each to an `OutboxMessage`, adds it to the `DbContext`, and removes just that event from the entity via the new `RemoveDomainEvent`.

`SaveChangesAsync()` then commits the business row and the new `OutboxMessage` row(s) in one transaction — closing the gap described in the Problem section.

The existing post-commit `DispatchDomainEventsAsync()` step is unchanged and still runs afterward — by the time it runs, reliable events have already been extracted, so it only ever dispatches non-reliable events (e.g. `ProvisionTenantOnRequestApprovedHandler`), synchronously, in-request, exactly as today. No regression to that path.

## Delivery: poll + immediate trigger

Two complementary mechanisms, so latency and reliability are handled by two different, independently-failing paths:

Both paths ultimately call the same `IOutboxRelayService`, just with a different selection of rows — there is one relay implementation, not two competing ones:

1. **Immediate trigger (latency):** right after `SaveChangesAsync()` commits, `UnitOfWork.CompleteAsync` calls `IOutboxTrigger.NotifyAsync(newOutboxMessageIds, ct)` — a Domain-defined port, injected the same way `IDomainEventDispatcher` already is, so `Core.Persistence` gains zero new package or project reference. The call is wrapped in its own try/catch that only logs on failure and never rethrows — this is purely an optimization, not the reliability guarantee. `HangfireOutboxTrigger` (`Infrastructure`) is the only place that touches `IBackgroundJobClient.Enqueue(...)`; it enqueues a lightweight fire-and-forget job scoped to just those specific `OutboxMessage` IDs, which calls `IOutboxRelayService.ReplayAsync(ids)`.
2. **Recurring poll (reliability):** a Hangfire `RecurringJob` (e.g. every 30–60s) calls `IOutboxRelayService.ReplayPendingAsync()` — the same service, but selecting *all* remaining `Pending` rows rather than a specific ID list. This catches anything the immediate trigger missed (e.g. a crash between commit and the `NotifyAsync` call, or Hangfire storage briefly unavailable at that moment) and is what guarantees nothing is ever permanently lost even if the fast path fails. The per-message concurrency/retry/error-handling rules below apply identically to both entry points, since both end up in the same service.

## `IOutboxRelayService` mechanics

Since both the immediate trigger and the recurring sweep call into the same service, a single message could in principle be picked up by both at once (an immediate job still processing a row when the recurring sweep also selects it) — this needs to be prevented at the row level, not just at the job-scheduling level.

- **Atomic claim:** before processing a message, the service claims it with a conditional update — `WHERE Id = @id AND Status = 'Pending'`, via EF Core's `ExecuteUpdateAsync`, setting `Status = 'Processing'` — and checks the affected-row count. If it's 0, another caller already claimed that row first; skip it silently. This closes the race between the two entry points without needing distributed locks.
- **`[DisableConcurrentExecution]`** on the recurring job additionally prevents two overlapping *sweep* runs if a batch takes longer than the poll interval — a cheap extra guard on top of the row-level claim, not a substitute for it.
- **Per-message error handling:** each message in a batch is processed inside its own try/catch — one bad message never fails the whole batch or causes already-succeeded messages to be reprocessed.
  - Success → `Status = Processed`, `ProcessedAtUtc` set.
  - Failure → `RetryCount` incremented, `LastError` recorded, `Status` reverts from `Processing` back to `Pending` so the next poll picks it up again — until a max retry count (5) is hit, at which point `Status = Failed` and auto-retry stops. This is the terminal state the future SuperAdmin failed-job dashboard will read and offer manual retry against by `JobId` — out of scope here, but the data shape is ready for it.
- **Outer safety net:** a small global `AutomaticRetryAttribute` (2–3 attempts, backoff) covers the rarer case where the job method itself throws (e.g. a DB connectivity blip reading the batch) — separate from the per-message retry logic above.

## Hangfire setup

- **Storage:** `Hangfire.SqlServer`, using the same `DefaultConnection` as `ApexBookingDbContext`, in its own schema (e.g. `hangfire`) — one database to operate, no second connection string.
- **Registration:** inside `AddInfrastructureService` (`Infrastructure/Dependency/InfrastructureDependencies.cs`), called from `WebApi/Program.cs` — same convention already used for `BackgroundWorker`.
- **Dashboard:** exposed at `/hangfire` in this phase. Auth via a custom `IDashboardAuthorizationFilter` (Hangfire's own extension point — the dashboard is raw middleware, not an MVC endpoint, so ASP.NET's `[Authorize]` attribute doesn't apply directly) that checks `httpContext.User.HasClaim("platform_admin", "true")` — the same claim the existing `SuperAdminOnly` policy already checks (`AuthenticationExtensions.cs`), just expressed the way Hangfire requires. Lives in `Infrastructure` alongside the rest of the Hangfire wiring; mounted from `Program.cs` via one extension method. Must be mounted **after** `app.UseAuthentication()` in the middleware pipeline, or `HttpContext.User` won't be populated when the filter runs.

## Testing

Matches the existing test project's conventions (`ApexBooking.Core.Domain.UnitTests` — xUnit, `Microsoft.EntityFrameworkCore.InMemory`, already references every layer; same style as the existing `DomainEventPipelineTests.cs`):

- **`OutboxRelayService` (Application):** fake `IPublisher` spy plus EF InMemory for the claim step — covers: deserializing a stored `EventType`/`Payload` back to the correct concrete domain event and calling `Publish` with it; the atomic-claim race (two concurrent calls targeting the same row, only one should win and publish); per-message try/catch isolation within a batch; `RetryCount`/`LastError`/terminal-`Failed`-after-5 lifecycle. This is where the real logic lives, so it's where most of the test coverage goes.
- **`UnitOfWork.CompleteAsync` split logic (Persistence):** EF InMemory — an entity raising an `IReliableDomainEvent` produces exactly one `OutboxMessage` row after `CompleteAsync` and is not also fired through the immediate post-commit path; a non-reliable event still dispatches synchronously exactly as today (regression check for `ProvisionTenantOnRequestApprovedHandler`).
- **Hangfire recurring job class (Infrastructure):** thin — just calls `IOutboxRelayService.ReplayPendingAsync()` under `[DisableConcurrentExecution]`. Not much to test beyond "it calls the service," since the actual behavior is covered above.
- **`HangfireOutboxTrigger`:** thin adapter over `IBackgroundJobClient.Enqueue`, same category as the already-untested `FcmPushNotificationService` adapter — no dedicated unit tests beyond what `OutboxRelayService`'s tests already cover.

## Out of scope

- `TrialExpiryJob`/`TrialExpiryWorker` — left commented out, untouched, for its own future spec.
- FCM removal / SignalR bell replacement — future spec.
- SuperAdmin failed-job monitoring dashboard UI — future spec; this design only produces the `Status = Failed` / `LastError` / `RetryCount` data shape it will need.
