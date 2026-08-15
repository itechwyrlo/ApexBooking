# Background Jobs: Current State Audit

**Type:** Read-only audit — no code changes were made as part of this document.

## Question

Does ApexBooking have a working background job system? If so, does it handle push notifications, scheduled emails, or both?

## Short answer

There effectively isn't a working background job today. The infrastructure for a generic background worker exists and is registered, but nothing feeds it, and the one scheduled job written for it is entirely commented out. Push notifications and transactional emails are handled by two separate mechanisms, and neither goes through the "background job" system.

## 1. The generic background worker (`ApexBooking.Infrastructure/BackgroundJobs/`)

- **`BackgroundTaskQueue.cs`** — an in-memory `Channel<Func<CancellationToken,Task>>` queue (capacity 100). Registered as a singleton.
- **`BackgroundWorker.cs`** — an `IHostedService`/`BackgroundService` that loops forever, dequeuing and running whatever work items land in that queue. Registered via `AddHostedService<BackgroundWorker>()`.
- **Nobody enqueues anything.** The only caller of `IBackgroundTaskQueue.QueueAsync(...)` anywhere in the codebase is `TrialExpiryWorker`, which is fully commented out. So this hosted service is live at runtime but sits idle forever, waiting on an empty channel — dead infrastructure right now.

- **`TrialExpiryJob.cs`** and **`TrialExpiryWorker.cs`** — entirely commented out (every line, not just disabled logic). This was the one *scheduled* job in the system: an hourly loop (`Task.Delay(1hr)`) that would:
  - suspend tenants whose trial had expired (send an email + create `Notification` records + push + realtime),
  - send "trial ends in 3 days" reminders (same pattern).
  - Its DI registrations (`AddScoped<TrialExpiryJob>()`, `AddHostedService<TrialExpiryWorker>()`) are also commented out in `InfrastructureDependencies.cs:76-78`. None of this runs today.

## 2. What actually sends emails today: domain-event side effects, not a job

Emails are triggered synchronously, in-process, as part of the request/command that raises the domain event — via the MediatR domain-event pipeline (`DomainEventDispatcher` → `IPublisher.Publish`, invoked from `UnitOfWork.CompleteAsync`, i.e. on `SaveChanges`). This is **not** scheduled and **not** queued to the background worker — it runs inline and is awaited before the request completes.

Live handlers found (all `INotificationHandler<DomainEventNotification<...>>`):

- `SendBookingConfirmationEmailHandler` (booking scheduled)
- `SendThankYouEmailOnBookingCompletedHandler` (booking completed)
- `SendStaffSetupInvitationOnTeamMemberInvitedHandler`
- `SendTenantRequestReceivedEmailHandler`
- `SendOwnerSetupInvitationOnTenantCreatedHandler`
- `SendTenantRequestRejectedEmailHandler`

These call `IBookingNotificationService` / `INotificationService` → `EmailService` / `BrevoSmtpService` (SMTP via Brevo). All synchronous, transactional-request-scoped email — no scheduling, no retry queue, no delay.

## 3. Push notifications: wired but currently unused

- `IPushNotificationService` → `FcmPushNotificationService` (Firebase Cloud Messaging) is registered in DI and fully implemented (sends to FCM tokens, prunes stale/unregistered tokens on failure).
- `IRealtimeNotificationService` → `SignalRNotificationService` is also registered and implemented.
- The **only** call site for either of them, anywhere in the codebase, was inside the commented-out `TrialExpiryJob`. Right now, push and realtime notifications are dead code paths — implemented and DI-wired, but never invoked by any live handler.

## Summary table

| Mechanism | Purpose | Trigger | Status |
|---|---|---|---|
| `BackgroundWorker` + `BackgroundTaskQueue` | Generic fire-and-forget queue processor | In-process channel | Registered, running, **but empty** — nothing enqueues to it |
| `TrialExpiryJob` / `TrialExpiryWorker` | Hourly job: expire trials, send reminder/expiry emails + push + notifications | `Task.Delay(1h)` loop | **Fully commented out**, DI registration also commented out |
| Domain-event email handlers (6 of them) | Transactional emails (booking confirmation, thank-you, invitations, tenant-request emails) | MediatR notification, fired synchronously from `UnitOfWork.CompleteAsync` | **Live and active** — but this is inline request-scoped side-effect dispatch, not a scheduled/background job |
| `FcmPushNotificationService` / `SignalRNotificationService` | Push notifications / realtime | Called from `IPushNotificationService` / `IRealtimeNotificationService` | Implemented + DI-registered, but **no live caller** — only referenced by the disabled `TrialExpiryJob` |

## Key files

- `ApexBooking.Infrastructure/BackgroundJobs/BackgroundWorker.cs`
- `ApexBooking.Infrastructure/BackgroundJobs/BackgroundTaskQueue.cs`
- `ApexBooking.Infrastructure/BackgroundJobs/TrialExpiryWorker.cs` (commented out)
- `ApexBooking.Infrastructure/BackgroundJobs/TrialExpiryJob.cs` (commented out)
- `ApexBooking.Infrastructure/Dependency/InfrastructureDependencies.cs:73-78` (registration block)
- `ApexBooking.Core.Application/Common/DomainEvent/DomainEventDispatcher.cs`
- `ApexBooking.Core.Persistence/UnitOfWork.cs:73-74` (dispatch call site — fires on `SaveChanges`)
- `ApexBooking.Core.Application/Features/{Bookings,Tenancy,TenantRequest}/Events/Send*Handler.cs` (6 live email handlers)
- `ApexBooking.Infrastructure/ExternalServices/Push/FcmPushNotificationService.cs`
- `ApexBooking.Infrastructure/ExternalServices/Realtime/SignalRNotificationService.cs`

## Out of scope

No changes were made. This document only records current state as of 2026-08-07.
