# Architecture Violations Tracker

Work log for resolving CLAUDE.md violations. Address one violation at a time: present problem → define plan → get approval → implement → verify build → proceed.

---

## Legend

- `[DONE]` — Resolved and build-verified
- `[PENDING]` — Identified, not yet addressed
- `[NEXT]` — Current violation awaiting approval or in-progress

---

## DONE

---

### V-01 — Staffs Commands: BaseResponse + BusinessRuleBrokenException + domain entity exposure

**Rule violated:** Forbidden everywhere: BaseResponse, Result, Success or Failure wrappers. Forbidden in handlers: throwing domain exceptions. Application layer must not expose domain entities across layer boundaries.

**Files fixed:**
- `ApexBooking.Core.Domain/Entities/Tenant.cs` — Added `EnforcePlanStaffLimit(int existingCount)`
- `ApexBooking.Core.Application/Features/Staffs/Commands/CreateStaff/CreateStaffCommand.cs` — Changed `ICommand<BaseResponse<Staff>>` → `ICommand<StaffDto>`
- `ApexBooking.Core.Application/Features/Staffs/Commands/CreateStaff/CreateStaffHandler.cs` — Returns `StaffDto`, calls `tenant.EnforcePlanStaffLimit()`, throws `NotFoundException`
- `ApexBooking.Core.Application/Features/Staffs/Commands/UpdateStaff/UpdateStaffCommand.cs` — Changed `ICommand<BaseResponse<StaffId>>` → `ICommand<StaffDto>`
- `ApexBooking.Core.Application/Features/Staffs/Commands/UpdateStaff/UpdateStaffCommandHandler.cs` — Returns `StaffDto`, throws `NotFoundException`
- `ApexBooking.Core.Application/Features/Staffs/Commands/DeactivateStaff/DeactivateStaffCommand.cs` — Changed to `ICommand` (void)
- `ApexBooking.Core.Application/Features/Staffs/Commands/DeactivateStaff/DeactivateStaffCommandHandler.cs` — Changed to `ICommandHandler<DeactivateStaffCommand>` (void), throws `NotFoundException`
- `ApexBooking.WebApi/Controllers/StaffController.cs` — Updated `Create` action to use `StaffDto` directly

---

## DONE

---

### V-10 — Remaining Query Handlers: `BaseResponse` wrapper (batch)

**Rule violated:** BaseResponse forbidden everywhere.

The following features contain query/command handlers that still use `BaseResponse<T>`. Each will be fixed with the same pattern: remove wrapper, return DTO directly, replace `Failure(...)` with `NotFoundException`.

**Sub-items:**
- **V-10a — Settings Queries:** `GetTenantSettingsQueryHandler`, `GetTenantPaymentPolicyQueryHandler`, `GetPayPalSettingsHandler` ✓ DONE
- **V-10b — Tenants:** `GetTenantProfileQueryHandler`, `UpdateTenantProfileCommandHandler` ✓ DONE
- **V-10c — Bookings:** `GetBookingByIdQueryHandler`, `ValidateCancellationTokenQueryHandler` ✓ DONE
- **V-10d — Public:** `GetPublicTenantQueryHandler`, `GetPublicBookingByIdQueryHandler`, `GetMonthlyAvailabilityQueryHandler`, `GetPublicResourcesQueryHandler` ✓ DONE
- **V-10e — Staffs Queries:** `GetStaffAvailabilityQueryHandler` ✓ DONE | `GetStaffExceptionsQuery` — N/A (uses `PagedResult`, not `BaseResponse`)
- **V-10f — Availability:** `GetAvailableSlotsQueryHandler` ✓ DONE (also fixed V-11 in same pass)
- **V-10g — SuperAdmin Queries + Commands:** `GetPlatformOverviewQueryHandler`, `GetOrganizationDetailQueryHandler`, `GetPlatformPaymentGatewayQueryHandler`, `GetTenantRequestsQueryHandler`, `GetTenantRequestByIdQueryHandler`, `CreateOrganizationCommandHandler`, `AssignExistingUserCommandHandler`, `ConfigurePlatformPaymentGatewayHandler` ✓ DONE — Domain guards `Tenant.EnsureSlugIsAvailable`, `Tenant.EnsureOwnerEmailIsAvailable` added; `AssignExistingUserCommandValidator` added; `ConfigurePlatformPaymentGateway` credential-failure throws `BusinessRuleBrokenException` (subordinate to existing infrastructure violation — HTTP call in Application)
- **V-10h — Payment:** `InitiatePaymentHandler`, `HandlePayPalWebhookHandler` ✓ DONE — `Booking.EnsureAwaitingPayment()` domain guard added; `HandlePayPalWebhookCommand` → `ICommand` (void); `WebhooksController` updated; infrastructure PayPal failures throw `InvalidOperationException` (subordinate to existing HTTP-in-Application violation)
- **V-10i — Auth (missed):** `LoginSuperAdminCommandHandler`, `AccountVerificationCommandHandler` ✓ DONE — `SuperAdmin.EnsureIsActive()` domain guard added; `User.EnsureInvitationNotExpired()` domain guard added; null/wrong-password → `UnauthorizedException`; token failure → `UnauthorizedException`; transport URL construction in `AccountVerificationCommandHandler` retained (noted subordinate violation)
- **V-10j — Settings Commands (missed from V-10a):** `UpdateTenantSettingsHandler`, `UpdateTenantPaymentPolicyCommandHandler` ✓ DONE
- **V-10k — Services Queries (missed):** `GetServiceByIdQueryHandler` ✓ DONE — null service → `NotFoundException` added (was silently crashing before)

**Plan:** Read each handler, remove `BaseResponse<T>`, return DTO directly, replace `Failure(...)` with `NotFoundException` or appropriate domain guards, update corresponding controller actions that unwrap `.Data`. Work feature-by-feature, present sub-items one at a time for approval.

---

## DONE

---

### V-09 — Staffs Queries: `BaseResponse` wrapper + stale `ResourceId` type reference

**Rule violated:** BaseResponse forbidden everywhere. `ResourceId` type no longer exists — entity was renamed to Staff.

**Files fixed:**
- `GetStaffByIdQuery.cs` → `IQuery<StaffDto>`; removed duplicate using and `SharedKernel.Models`
- `GetResourceByIdQueryHandler.cs` → renamed class to `GetStaffByIdQueryHandler`; namespace corrected to `Features.Staffs.Queries.GetStaffById`; `new ResourceId(...)` → `new StaffId(...)`; returns `StaffDto` directly; not-found → `NotFoundException`

---

### V-08 — Availability Commands: `BaseResponse` wrapper + `BusinessRuleBrokenException` from handler

**Rule violated:** BaseResponse forbidden everywhere. `BusinessRuleBrokenException` must only be thrown from Domain layer.

**Files fixed:**
- `AddExceptionCommand.cs` → `ICommand` (void)
- `AddExceptionHandler.cs` → void handler; not-found → `NotFoundException`; removed `BaseResponse`
- `RemoveExceptionCommand.cs` → `ICommand` (void)
- `RemoveExceptionHandler.cs` → void handler; fixed `staffId is null` → `staff is null` bug; not-found → `NotFoundException`; removed `BaseResponse` and `BusinessRuleBrokenException`
- `SetResourceAvailabilityHandler.cs` → not-found `BusinessRuleBrokenException` → `NotFoundException`
- `StaffController.cs` → `AddException` and `RemoveException` return `NoContent()`

---

---

### V-07 — Services Commands: `BaseResponse` wrapper + `BusinessRuleBrokenException` from handler

**Rule violated:** BaseResponse forbidden everywhere. BusinessRuleBrokenException must only be thrown from Domain layer.

**Files fixed:**
- `Tenant.cs` — Added `EnforcePlanServiceLimit(int existingCount)` domain guard
- `Service.cs` — Added `static EnsureNameIsUnique(bool nameExists)` domain guard
- `CreateServiceCommand.cs` → `ICommand<ServiceDto>`
- `CreateServiceHandler.cs` → returns `ServiceDto`; tenant not-found → `NotFoundException`; plan limit → `tenant.EnforcePlanServiceLimit(...)`; name duplicate → `Service.EnsureNameIsUnique(...)`
- `UpdateServiceCommand.cs` → `ICommand<ServiceDto>`
- `UpdateServiceHandler.cs` → returns `ServiceDto`; not-found → `NotFoundException`
- `DeactivateServiceCommand.cs` → `ICommand` (void)
- `DeactivateServiceHandler.cs` → void handler; not-found → `NotFoundException`
- `ServiceController.cs` — `Create`: `response.Data.Id` → `response.Id`; `Deactivate`: `Ok(response)` → `NoContent()`

---

### V-06 — Public: `SubmitTenantRequest` BaseResponse wrapper

**Rule violated:** BaseResponse forbidden everywhere. Dead `Enum.TryParse` failure branch in handler (validator already guards the value).

**Files fixed:**
- `TenantRequest.cs` — Added `EnsureNoPendingRequest(bool alreadyExists)` static domain guard
- `SubmitTenantRequestCommand.cs` — `ICommand<BaseResponse<Guid>>` → `ICommand<Guid>`
- `SubmitTenantRequestCommandHandler.cs` — Returns `Guid` directly; `BaseResponse.Failure(...)` → `TenantRequest.EnsureNoPendingRequest(...)`; `Enum.TryParse` + failure branch → `Enum.Parse` (safe, validator guards it)

---

### V-05 — SuperAdmin Commands: `BaseResponse` wrapper

**Rule violated:** "Forbidden everywhere: BaseResponse, Result, Success or Failure wrappers, Boolean business failure returns."

**Files fixed:**
- `TenantRequest.cs` — Added `EnsureSlugIsAvailable(bool slugTaken)` and `EnsureOwnerEmailIsAvailable(bool existsAsTenant, bool existsAsUser)` domain guards
- `Tenant.cs` — Added `EnsureUserEmailIsNotRegistered(bool emailTaken)` domain guard
- `ApexBooking.Core.Application/Common/Validators/CreateTenantUserCommandValidator.cs` — New validator; validates Role string so handler can use `Enum.Parse` safely
- `ApproveTenantRequestCommand.cs` → `ICommand` (void)
- `ApproveTenantRequestCommandHandler.cs` → void handler; `Failure(...)` paths → `NotFoundException` / domain guards; Identity failures → `InvalidOperationException` (subordinate to known boundary violation)
- `RejectTenantRequestCommand.cs` → `ICommand` (void)
- `RejectTenantRequestCommandHandler.cs` → void handler; not-found → `NotFoundException`
- `ResendInvitationCommand.cs` → `ICommand` (void)
- `ResendInvitationCommandHandler.cs` → void handler; not-found → `NotFoundException`; already-activated → `user.EnsureNotYetActivated()`
- `CreateTenantUserCommand.cs` → `ICommand<TenantUserDto>`
- `CreateTenantUserCommandHandler.cs` → returns `TenantUserDto`; not-found → `NotFoundException`; duplicate email → `tenant.EnsureUserEmailIsNotRegistered()`; Identity failures → `InvalidOperationException`
- `SuperAdminController.cs` — Approve/Reject/ResendInvite → `NoContent()`; CreateTenantUser → `Created(..., result)`

---

### V-04 — Auth / SuperAdmin / Public: Inline HTML email templates inside Application handlers

**Rule violated:** "Email template generation must not live inside handlers. Reusable templates belong in dedicated template services."

**Files fixed:**
- `ApexBooking.Core.Domain/Services/Notification/Auth/IAuthNotificationService.cs` — New interface: `SendPasswordResetEmailAsync`, `SendAccountApprovalEmailAsync`, `SendRejectionEmailAsync`, `SendInvitationEmailAsync`, `SendInvitationResentEmailAsync`, `SendTenantRequestReceivedEmailAsync`
- `ApexBooking.Infrastructure/ExternalServices/AuthNotificationService/AuthNotificationService.cs` — New implementation wrapping `INotificationService`; all HTML template strings live here
- `InfrastructureDependencies.cs` — Registered `IAuthNotificationService → AuthNotificationService`
- `ForgotPasswordCommandHandler.cs` — Replaced inline HTML + `_notification.SendEmailAsync` with `_authNotification.SendPasswordResetEmailAsync`
- `ApproveTenantRequestCommandHandler.cs` — Replaced with `_authNotification.SendAccountApprovalEmailAsync`
- `RejectTenantRequestCommandHandler.cs` — Replaced with `_authNotification.SendRejectionEmailAsync`
- `ResendInvitationCommandHandler.cs` — Replaced with `_authNotification.SendInvitationResentEmailAsync`
- `CreateTenantUserCommandHandler.cs` — Replaced with `_authNotification.SendInvitationEmailAsync`
- `SubmitTenantRequestCommandHandler.cs` — Replaced with `_authNotification.SendTenantRequestReceivedEmailAsync`

---

### V-03 — Auth Commands: `BusinessRuleBrokenException` thrown from Application handlers

**Rule violated:** "Throwing domain exceptions (e.g. BusinessRuleBrokenException) — these belong exclusively in the Domain layer."

**Files fixed:**
- `ApexBooking.Core.Domain/Entities/User.cs` — Added `EnsureNotYetActivated()` guard method; throws `BusinessRuleBrokenException("This account has already been activated.")` if `Status != UserStatus.Invited`
- `AcceptInvitationCommandHandler.cs` — Replaced inline `throw new BusinessRuleBrokenException(...)` with `user.EnsureNotYetActivated()`

**Subordinate known violations (not expanded):**
- `AcceptInvitationCommandHandler.cs:51` — Identity password result failure; subordinate to known `User : IdentityUser` boundary violation
- `ResetPasswordCommandHandler.cs:43` — Same

---

---

### V-02 — Build-Blocking: Wrong `INotificationService` namespace + controller sync

**Rule violated:** Compilation errors. Interface is at `ApexBooking.Core.Domain.Services.EmailNotification` but all consumers imported `ApexBooking.Core.Domain.Services.Notification`. Additionally, 3 controllers retained `BaseResponse`-style unwrapping for commands already changed to void/DTO returns.

**Files fixed:**
- `ForgotPasswordCommandHandler.cs` — namespace corrected
- `ApproveTenantRequestCommandHandler.cs` — namespace corrected
- `RejectTenantRequestCommandHandler.cs` — namespace corrected
- `ResendInvitationCommandHandler.cs` — namespace corrected
- `CreateTenantUserCommandHandler.cs` — namespace corrected
- `SubmitTenantRequestCommandHandler.cs` — namespace corrected
- `TrialExpiryJob.cs` — namespace corrected
- `BrevoSmtpService.cs` — namespace corrected
- `InfrastructureDependencies.cs` — namespace corrected
- `BookingController.cs` — `CreateBooking` uses `result.BookingId` directly; `Cancel` returns `NoContent()`
- `AuthController.cs` — `ResetPassword` returns `NoContent()`
- `PublicController.cs` — `CancelByToken` returns `NoContent()`

---

## PENDING

---

### V-11 — Mapping Rule: `AvailabilityMappings` class in wrong location ✓ DONE

**Files fixed:**
- `Dtos/AvailableSlotsDto.cs` — Removed `AvailabilityMappings` class
- `mapper/AvailabilityMapping.cs` — New file; `AvailabilityMappings` class moved here under `ApexBooking.Core.Application.Resources.Mappings`
- `GetAvailableSlotsQueryHandler.cs` — `using` updated to `ApexBooking.Core.Application.Resources.Mappings` (fixed as part of V-10f)

---

## Notes

- **Known boundary violations** (do not expand): Domain references ASP.NET Identity, `User` inherits `IdentityUser`, `IUnitOfWork` leaks Identity concerns, email HTML generation existed in `CreateBookingCommandHandler` (already documented).
- **Build gate:** Always run `dotnet build ApexBooking.sln` after each violation fix before proceeding to the next.
- **Approval protocol:** State violation → define plan → get explicit approval → implement → build-verify → proceed.
