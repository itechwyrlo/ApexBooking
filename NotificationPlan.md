# Notification System — Investigation & Implementation Plan

## Status: INVESTIGATION COMPLETE — OPEN QUESTIONS RESOLVED — READY TO IMPLEMENT

---

## Investigation Summary

### Frontend — Current State

**Header.tsx (Tenant Admin header)**
- Notification bell dropdown is a complete static placeholder.
- `notifOpen` toggle state exists (line 27). Dropdown renders on click (lines 80–100).
- Hardcoded "0 new" badge and "No notifications yet" body. Zero data fetch.
- No hook, no type, no connection to any backend endpoint.
- File: `src/components/layout/Header.tsx`

**SuperAdminLayout.tsx (Super Admin top bar)**
- NO notification bell exists anywhere in this layout.
- Top bar (`lines 104–115`) contains only: hamburger toggle + "Platform Administration" label.
- A bell icon and dropdown must be added to this layout before super admin notifications can work.
- File: `src/components/layout/SuperAdminLayout.tsx`

**SettingsPage.tsx**
- The word "Notifications" appears once (line 264) as a label for an email toggle setting. Not in-app notifications. Not relevant.

**No notification hooks, types, or components exist anywhere in the frontend.**

---

### Backend — Current State

**Email notification infrastructure (exists, complete, separate concern):**
- `INotificationService` — `SendEmailAsync(to, subject, content)` — email transport only
- `IBookingNotificationService` — booking confirmation email
- `IAuthNotificationService` — password reset, approval, rejection, invitation emails

**In-app notification infrastructure (does NOT exist at all):**
- No `Notification` entity
- No `INotificationRepository`
- No notification-related CQRS commands or queries
- No notification controller or endpoints
- `NotificationLogId` value object already exists in `Domain/ValueObjects/ValueObjectsIdentifier.cs` line 46 — anticipated but never implemented. Reserved for a future audit log concept. Do NOT repurpose it for this feature — create a new `NotificationId` value object.

---

### Background Job Relevance

`TrialExpiryJob` lives in `ApexBooking.Infrastructure/BackgroundJobs/TrialExpiryJob.cs`.

Two methods:
- `SuspendExpiredTrialsAsync` — suspends tenants past trial end date, sends expiry email. Errors are logged via `ILogger`.
- `SendExpiryRemindersAsync` — sends 3-day warning email. Errors are logged via `ILogger`.

**YES, this job is directly relevant.** It must write super admin notification records for:
1. Each tenant whose trial expired and was suspended.
2. Each tenant whose reminder email was sent.
3. A single "job failed" notification when a per-tenant operation throws (write this in the catch block, separate `CompleteAsync`).

Current catch blocks only call `_logger.LogError(...)`. The notification write must be added there without disrupting error isolation.

---

### Recipient Identity

**Tenant Admin:**
- Entity: `User` in `Domain/Entities/User.cs`
- Has `TenantId` and inherits `IdentityUser<Guid>` (its `Id` is the ASP.NET Identity GUID)
- `IUserContextService.GetCurrentUserId()` returns this Identity `Guid`
- Notification scoped by `TenantId` + `RecipientId = User.Id`

**Super Admin:**
- Entity: `SuperAdmin` in `Domain/Entities/SuperAdmin.cs`
- Also inherits `IdentityUser<Guid>`. Has both `SuperAdminId` (domain value object) and `IdentityUser.Id` (ASP.NET identity GUID).
- **CONFIRMED:** `TokenService.GenerateAccessToken(SuperAdmin)` writes `superAdmin.SuperAdminId.Value` into the `sub` claim. `UserContextService.GetCurrentUserId()` reads `ClaimTypes.NameIdentifier` which maps from `sub`. Therefore `GetCurrentUserId()` returns `SuperAdminId.Value` for super admins.
- `RecipientId = superAdmin.SuperAdminId.Value` for all super admin notifications.
- **TrialExpiryJob exception:** No HTTP context — `IUserContextService` is not usable. Must query `ISuperAdminRepository.GetAllAsync(sa => sa.IsActive)` directly to get recipient IDs.
- Notifications are NOT tenant-scoped (`TenantId = null`).

---

## Architecture Decisions

### Entity name and identity
- Entity: `Notification` (not `NotificationLog` — this is interactive, not an audit log)
- Identity: new `NotificationId` value object (add to `Domain/ValueObjects/ValueObjectsIdentifier.cs`)
- The existing `NotificationLogId` (line 46) is left untouched — it is reserved for a different purpose

### Aggregate design
`Notification` implements `IAggregateRoot` only (not `ITenantEntity`) because super admin notifications have no tenant. `TenantId?` is nullable.

```csharp
public class Notification : IAggregateRoot
{
    public NotificationId NotificationId { get; private set; }
    public Guid RecipientId { get; private set; }           // User.Id or SuperAdmin.Id
    public NotificationRecipientType RecipientType { get; private set; }
    public TenantId? TenantId { get; private set; }         // null for SuperAdmin
    public NotificationEventType EventType { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    // Domain methods: MarkRead(), static Create(...)
}
```

### NotificationRecipientType enum
```csharp
public enum NotificationRecipientType { TenantAdmin, SuperAdmin }
```

### NotificationEventType enum (strong typing, no magic strings)
```csharp
public enum NotificationEventType
{
    // Tenant Admin
    BookingCreated,
    BookingConfirmed,      // deferred — handler does not exist yet
    BookingCancelled,
    BookingCompleted,      // deferred — handler does not exist yet
    BookingNoShow,         // deferred — handler does not exist yet
    BookingPendingPayment, // deferred — same as BookingCreated when price > 0
    PaymentCaptured,
    StaffCreated,
    StaffDeactivated,

    // Super Admin
    TenantRequestSubmitted,
    TenantRequestApproved,
    TenantRequestRejected,
    TrialReminderSent,
    TrialExpiredSuspended,
    BackgroundJobFailed
}
```

### Where notifications are written
- **Application handlers:** after domain action succeeds, inside the same `IUnitOfWork` transaction before `CompleteAsync()`.
- **TrialExpiryJob:** after each per-tenant operation, inside the per-tenant try/catch. For job failures: inside the catch block with a separate `CompleteAsync()` call.
- **NOT** from Domain entities — domain does not know about notification side effects.

### Tenant Admin recipient resolution
Query `IUserRepository.GetAllAsync(u => u.TenantId == tenantId && u.Role == UserRole.TenantAdmin && u.Status == UserStatus.Active)` inside the handler, then write one `Notification` record per admin user. Typical case is one admin.

### Super Admin recipient resolution
Query `ISuperAdminRepository.GetAllAsync(sa => sa.IsActive)`, write one `Notification` per active super admin. MVP has one super admin.

---

## Implementation Plan

### OPEN QUESTIONS — ALL RESOLVED FROM CODE

1. **Super Admin JWT claim** — CONFIRMED: `SuperAdminId.Value` is in the `sub` claim (`TokenService.cs` line 49). `GetCurrentUserId()` reads `ClaimTypes.NameIdentifier` which maps from `sub`. Returns `SuperAdminId.Value` for super admins. For TrialExpiryJob, query `ISuperAdminRepository.GetAllAsync(sa => sa.IsActive)` directly (no HTTP context available in background jobs).

2. **Multi-admin tenant notifications** — CONFIRMED: Notify ALL active TenantAdmin users in the tenant. `IUserRepository.GetUsersByRoleAsync(TenantId, UserRole.TenantAdmin)` already exists — use it. No new repository method needed.

3. **Notification retention policy** — DECIDED: Keep all records. Repository `GetLatestAsync` returns top 10 ordered by `CreatedAt DESC`. No cleanup job in MVP.

4. **Unread count source** — DECIDED: Include `unreadCount` in the summary DTO computed from a separate `GetUnreadCountAsync` DB query. The badge must reflect ALL unread (not just within top 10), so `items.filter(!isRead).length` would be wrong when there are >10 unread.

5. **`timeFormat.ts` utility** — CONFIRMED EXISTS at `src/utils/timeFormat.ts`. Add `toRelativeTime(createdAt: string): string` there alongside existing `formatIsoDisplay` and `formatTo12Hour`.

---

### Phase 1 — Domain layer (5 files)

| # | File | Action | Purpose |
|---|---|---|---|
| 1 | `ApexBooking.Core.Domain/Entities/Notification.cs` | Create | Aggregate root, `Create()` factory, `MarkRead()` method |
| 2 | `ApexBooking.Core.Domain/Enums/NotificationEventType.cs` | Create | Event type enum (15 values) |
| 3 | `ApexBooking.Core.Domain/Enums/NotificationRecipientType.cs` | Create | `TenantAdmin`, `SuperAdmin` |
| 4 | `ApexBooking.Core.Domain/ValueObjects/ValueObjectsIdentifier.cs` | Edit | Add `record NotificationId(Guid Value)` |
| 5 | `ApexBooking.Core.Domain/Repositories/INotificationRepository.cs` | Create | `GetLatestAsync(recipientId, limit)`, `GetUnreadCountAsync(recipientId)`, `MarkAllReadAsync(recipientId)` |
| 6 | `ApexBooking.Core.Domain/Interfaces/IUnitOfWork.cs` | Edit | Add `INotificationRepository NotificationRepository` property |

Build gate after Phase 1.

---

### Phase 2 — Persistence layer (3 files + migration)

| # | File | Action | Purpose |
|---|---|---|---|
| 7 | `ApexBooking.Core.Persistence/Mappings/NotificationConfiguration.cs` | Create | EF Fluent API: table name, indexes on `RecipientId + IsRead + CreatedAt` |
| 8 | `ApexBooking.Core.Persistence/Repositories/NotificationRepository.cs` | Create | `INotificationRepository` implementation |
| 9 | `ApexBooking.Core.Persistence/Data/ApexBookingDbContext.cs` | Edit | Add `DbSet<Notification> Notifications` |

Then run: `dotnet ef migrations add AddNotificationTable --project ApexBooking.Core.Persistence`

Build gate after Phase 2.

---

### Phase 3 — Application DTOs + CQRS (5 files)

| # | File | Action | Purpose |
|---|---|---|---|
| 10 | `ApexBooking.Core.Application/Dtos/NotificationDto.cs` | Create | `NotificationId, EventType, Title, Message, IsRead, CreatedAt` |
| 11 | `ApexBooking.Core.Application/Dtos/NotificationSummaryDto.cs` | Create | `IReadOnlyList<NotificationDto> Items, int UnreadCount` |
| 12 | `Application/Features/Notifications/Queries/GetNotifications/GetNotificationsQuery.cs` | Create | Query record |
| 13 | `Application/Features/Notifications/Queries/GetNotifications/GetNotificationsQueryHandler.cs` | Create | Reads `IUserContextService` for recipientId, queries repo, returns `NotificationSummaryDto` |
| 14 | `Application/Features/Notifications/Commands/MarkAllRead/MarkAllReadCommand.cs` | Create | Command record |
| 15 | `Application/Features/Notifications/Commands/MarkAllRead/MarkAllReadCommandHandler.cs` | Create | Calls `NotificationRepository.MarkAllReadAsync(recipientId)` |

Build gate after Phase 3.

---

### Phase 4 — Handler modifications (9 handlers + TrialExpiryJob)

Add notification writes inside existing handlers. Each write uses `_unitOfWork.NotificationRepository.Add(notification)` before the existing `CompleteAsync()`.

**Tenant Admin notifications** (must resolve open question 2 first):

| Handler | File | Event | Message |
|---|---|---|---|
| `CreateBookingCommandHandler` | `Features/Bookings/Commands/CreateBooking/` | `BookingCreated` | "New booking by {Guest.FirstName} {Guest.LastName} for {ServiceName} with {StaffName} on {ScheduledDate:MMM d}." |
| `CancelBookingCommandHandler` | `Features/Bookings/Commands/CancelBooking/` | `BookingCancelled` | "Booking {BookingReference} was cancelled." |
| `HandlePayPalWebhookHandler` | `Features/Payment/Commands/HandlePayPalWebhook/` | `PaymentCaptured` | "Payment received for Booking {BookingReference}." |
| `CreateStaffHandler` | `Features/Staffs/Commands/CreateStaff/` | `StaffCreated` | "Staff record for {FirstName} {LastName} has been created." |
| `DeactivateStaffCommandHandler` | `Features/Staffs/Commands/DeactivateStaff/` | `StaffDeactivated` | "Staff record for {staff.FirstName} {staff.LastName} has been deactivated." |

**Deferred Tenant Admin events** (handlers do not exist yet — do NOT add):
- `BookingConfirmed` — no `ConfirmBookingCommandHandler`
- `BookingCompleted` — no `CompleteBookingCommandHandler`
- `BookingNoShow` — no `MarkNoShowCommandHandler`
- `BookingPendingPayment` — status is set inside `Booking.Create()`, not a separate handler; the `BookingCreated` notification message can indicate pending payment when `booking.Status == BookingStatus.PendingPayment`

**Super Admin notifications** (must resolve open question 1 first):

| Handler / Job | File | Event | Message |
|---|---|---|---|
| `SubmitTenantRequestCommandHandler` | `Features/Public/Commands/SubmitTenantRequest/` | `TenantRequestSubmitted` | "New tenant request from {BusinessName} is awaiting your approval." |
| `ApproveTenantRequestCommandHandler` | `Features/SuperAdmin/Commands/ApproveTenantRequest/` | `TenantRequestApproved` | "Tenant {BusinessName} has been approved and is now active." |
| `RejectTenantRequestCommandHandler` | `Features/SuperAdmin/Commands/RejectTenantRequest/` | `TenantRequestRejected` | "Tenant request from {BusinessName} has been rejected." |
| `TrialExpiryJob.SuspendExpiredTrialsAsync` | `Infrastructure/BackgroundJobs/TrialExpiryJob.cs` | `TrialExpiredSuspended` | "Tenant {BusinessName} trial has expired and has been suspended." |
| `TrialExpiryJob.SendExpiryRemindersAsync` | `Infrastructure/BackgroundJobs/TrialExpiryJob.cs` | `TrialReminderSent` | "Tenant {BusinessName} trial expires in 3 days. Reminder sent." |
| `TrialExpiryJob` catch block | `Infrastructure/BackgroundJobs/TrialExpiryJob.cs` | `BackgroundJobFailed` | "Background job failed: TrialExpiryJob encountered an error." |

**TrialExpiryJob modification note:** The job injects `IUnitOfWork` already. Add notification writes immediately after the successful `_unitOfWork.CompleteAsync(cancellationToken)` call for each tenant. For the failure case, write notification inside the catch block with a new `_unitOfWork.CompleteAsync()` call, isolated from the original failure.

Build gate after Phase 4.

---

### Phase 5 — API controller (1 file)

| # | File | Action | Purpose |
|---|---|---|---|
| 16 | `ApexBooking.WebApi/Controllers/NotificationController.cs` | Create | `[Authorize]` `GET api/notification` → `GetNotificationsQuery`, `PATCH api/notification/read-all` → `MarkAllReadCommand` |

Build gate after Phase 5.

---

### Phase 6 — Frontend (5 files)

| # | File | Action | Purpose |
|---|---|---|---|
| 17 | `features/notifications/types/index.ts` | Create | Mirror `NotificationDto` and `NotificationSummaryDto` |
| 18 | `features/notifications/hooks/useNotifications.ts` | Create | `axiosInstance.get<NotificationSummaryDto>('/notification')` — fetch on demand, polling interval configurable |
| 19 | `features/notifications/components/NotificationItem.tsx` | Create | Presentational: renders one notification row from DTO (title, message, relative timestamp, read indicator) |
| 20 | `components/layout/Header.tsx` | Edit | Replace static placeholder with `useNotifications` hook; unread badge from `data.unreadCount`; fetch on bell click; "Mark all read" calls `PATCH /notification/read-all` |
| 21 | `components/layout/SuperAdminLayout.tsx` | Edit | Add notification bell to top bar using same hook and same component |

**Note on relative timestamp:** Use a utility function `toRelativeTime(createdAt: string): string` in `utils/timeFormat.ts` (or create it if not present). Do not use a third-party library.

**Polling:** In `useNotifications`, add optional polling via `setInterval` with interval passed as a prop or defined as a constant. Default to no polling — fetch only on bell icon click. Polling can be enabled later.

Frontend type-check (`tsc --noEmit`) after Phase 6.

---

## Files NOT to modify
- `Domain/Services/Notification/` — email notification services are separate, leave untouched
- `INotificationService.cs`, `IBookingNotificationService.cs`, `IAuthNotificationService.cs` — email only, not affected

---

## Resume Checkpoint

**All questions resolved. Ready to implement.**

**Start at Phase 1, File 1:** Create `ApexBooking.Core.Domain/Entities/Notification.cs`

Implementation order is strict: Phase 1 → build → Phase 2 → migration → build → Phase 3 → build → Phase 4 → build → Phase 5 → build → Phase 6 → tsc --noEmit.
