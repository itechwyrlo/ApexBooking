# Background Jobs → Hangfire + Outbox: Architecture Reference

**Type:** Reference document for an in-progress multi-session effort. No implementation has started. Read this before brainstorming or planning the Hangfire/outbox work so decisions don't accidentally violate an existing project boundary.

Companion doc: [`2026-08-07-background-jobs-current-state-audit.md`](2026-08-07-background-jobs-current-state-audit.md) — what exists today (empty `BackgroundWorker`, disabled `TrialExpiryJob`, synchronous inline email handlers, unused FCM/SignalR).

## Where this effort is headed (decided so far)

- Replace the custom `BackgroundTaskQueue`/`BackgroundWorker` and the disabled `TrialExpiryJob`/`TrialExpiryWorker` with **Hangfire** as the single background-processing mechanism.
- Implement a **transactional outbox pattern** for domain events: write an `OutboxMessage` row in the same transaction as the business change, then a Hangfire recurring job (the "Outbox Relay") drains it and re-dispatches to the existing MediatR domain-event handlers (email today, SMS later — channel-agnostic).
- **Trial Expiry** becomes a Hangfire `RecurringJob` (cron), rebuilt from the currently-commented-out job — not touched/cleaned up until its own turn.
- **Push notification (item 5) is settled:** FCM/Firebase is dropped entirely. Replaced by: Hangfire job persists the `Notification` row → SignalR pushes it live to the bell icon for currently-connected users. Users not connected get nothing in the moment, only the row waiting next time they open the app — an accepted tradeoff.
- **Failed-job visibility for SuperAdmin:** not a job itself — a Hangfire state filter (`IElectStateFilter`/`IApplyStateFilter`) that catches any job landing in `Failed`, and persists job name, args, timestamp, and the actual exception into a table the SuperAdmin dashboard reads, with the Hangfire `JobId` retained so SuperAdmin can manually re-trigger it.
- One thing at a time — do not bundle these into one giant change.

## Layer structure (Clean/Onion, strict one-way dependencies)

```
SharedKernel (Framework.Core)     — base abstractions: IHasDomainEvents, IDomainEventDispatcher,
                                      ValueObjects, ITenantEntity. Depends on nothing else in-repo.
        ↑
GenericRepository.Abstractions    — repository interfaces, pure, depends on SharedKernel only.
        ↑
GenericRepository.EntityFramework — EF Core generic repo impl. Depends on Abstractions + SharedKernel.
        ↑
Core.Domain                       — entities, value objects, domain events, service *interfaces*
                                      (ports: IUnitOfWork, INotificationService, IPushNotificationService,
                                      IRealtimeNotificationService, IBackgroundTaskQueue...).
                                      Depends on SharedKernel + GenericRepository.Abstractions only.
        ↑                    ↑
Core.Application            Core.Persistence            Infrastructure
(MediatR handlers,          (DbContext, migrations,     (adapters: SMTP/Brevo, FCM, SignalR,
 commands/queries,          repos, UnitOfWork,           PayMongo, BackgroundJobs, Hubs).
 DTOs, validators,          Identity)                    Depends on Core.Domain ONLY.
 domain-event handlers      Depends on
 that send email)           GenericRepository.EF
 Depends on                 + Core.Domain ONLY.
 Core.Domain only.
        ↑                            ↑                            ↑
                          WebApi (composition root)
                 references ALL of the above; wires DI via one
                 Add<Layer>Services(services, config) extension per layer.
```

**The load-bearing rule: `Core.Persistence` and `Infrastructure` never reference `Core.Application`.** Neither project's `.csproj` has MediatR, AutoMapper, or FluentValidation — those only exist in `Core.Application`. `Persistence`'s `UnitOfWork` only knows `IDomainEventDispatcher` (the interface, defined in `SharedKernel`); it never touches the concrete `DomainEventDispatcher` (lives in `Application`, calls MediatR's `IPublisher`). WebApi is the only place concrete implementations are tied together, via DI, at the composition root.

## Package references per project (verified from `.csproj`, 2026-08-07)

| Project | Project references | Key package references |
|---|---|---|
| `SharedKernel` | (none) | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` |
| `GenericRepository.Abstractions` | SharedKernel | — |
| `GenericRepository.EntityFramework` | Abstractions, SharedKernel | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools` |
| `Core.Domain` | SharedKernel, GenericRepository.Abstractions | — |
| `Core.Application` | Core.Domain | `AutoMapper`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `MediatR` |
| `Core.Persistence` | GenericRepository.EntityFramework, Core.Domain | `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Configuration.Json` |
| `Infrastructure` | Core.Domain | `FirebaseAdmin`, `MailKit`, `MimeKit`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `FrameworkReference: Microsoft.AspNetCore.App` |
| `WebApi` | SharedKernel, Core.Application, Core.Persistence, Core.Domain, Infrastructure | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, Swagger/NSwag, SPA services |

`ApexBooking.Identity` project is retired (ADR-055) — folder still exists on disk but has no `.csproj` and is not in the `.sln`.

## Existing conventions to follow

- **Composition root pattern:** each layer exposes exactly one `Add<Layer>Services(IServiceCollection, IConfiguration)` extension — `AddApplicationServices` (Application), `AddPersistenceServices` (Persistence), `AddInfrastructureService` (Infrastructure) — all called in order from `WebApi/Program.cs`. `BackgroundWorker`/`BackgroundTaskQueue` registration currently lives inside `AddInfrastructureService`. Hangfire's `AddHangfire(...)` / `AddHangfireServer()` / dashboard wiring should follow the same shape, not go loose in `Program.cs`.
- **EF mapping convention:** entities live in `Core.Domain/Entities`; their `IEntityTypeConfiguration<T>` classes live in `Core.Persistence/Mappings`, auto-discovered via `builder.ApplyConfigurationsFromAssembly(...)` in `ApexBookingDbContext.OnModelCreating` — no manual per-entity wiring.
- **Tenant scoping:** a global EF query filter applies automatically to any entity implementing `ITenantEntity` (`ApplyGlobalFilters` in `ApexBookingDbContext`). Background/webhook code paths often have **no ambient tenant** at all — already called out in `SendBookingConfirmationEmailHandler`'s own comments. See [[feedback-tenant-scoping-idor]].
- **Domain-event dispatch today is NOT a real EF interceptor** — despite an earlier memory note calling it that. It's sequential code in `UnitOfWork.CompleteAsync`: `SaveChangesAsync()` runs, *then* `DispatchDomainEventsAsync()` fires MediatR notifications. No `ISaveChangesInterceptor` is registered anywhere in `PersistenceDependency.cs`.
- **Repository rule:** only `IAggregateRoot` entities get a dedicated repository — see [[feedback-aggregate-root-repository-rule]]. Relevant when deciding how `OutboxMessage`/failed-job-log rows get queried.

## Open friction points for the brainstorm (not resolved — decide deliberately)

1. **Database is SQL Server, not PostgreSQL.** Confirmed via `UseSqlServer(...)` in `PersistenceDependency.cs` and the `Microsoft.EntityFrameworkCore.SqlServer` package — no Npgsql anywhere in the repo. Any Hangfire reference material assuming Postgres (`Hangfire.PostgreSql`) needs `Hangfire.SqlServer` instead.
2. **True transactional outbox requires reordering `UnitOfWork.CompleteAsync`.** Writing `OutboxMessage` rows in the *same* transaction as the business row means either a real `ISaveChangesInterceptor` or adding the outbox rows to the context before the existing `SaveChangesAsync()` call — current dispatch happens after commit, which is exactly the gap the outbox is meant to close.
3. **The Outbox Relay job has nowhere to legally live yet.** The plan re-publishes domain events through MediatR so the existing 6 email handlers (in `Core.Application`) run unchanged. But `Infrastructure` (home of `BackgroundJobs/`) cannot reference `Core.Application` under the current boundary — no `IPublisher`. Needs an explicit call: relax the boundary for this one case, place the relay job in `Core.Application` itself (which would also need a `Hangfire.Core` package reference, currently absent from that layer), or introduce a new project.
4. **Tenant scoping of the new entities is undecided.** Is `OutboxMessage` tenant-scoped (`ITenantEntity`) or platform-wide? Same question for the failed-job-log entity backing the SuperAdmin view. Given the global query filter and the "no ambient tenant in background contexts" gotcha already on file, this needs a conscious answer, not a default.
5. **Minor existing drift worth fixing while we're in this exact area:** `IBackgroundTaskQueue.cs` physically sits under `Core.Domain/Services/` but is namespaced `ApexBooking.Core.Application.Interfaces` — a pre-existing boundary/namespace mismatch in the very abstraction this refactor replaces.
