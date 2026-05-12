# ApexBooking — Backend Feature Gap Analysis
# Generated: 2026-05-10
# Reference specs: feature_usecase.md, mvp.md

Legend:
  ✅ Complete
  🟡 Partial — exists but has gaps or bugs
  ❌ Not built

Progress:
  [██░░░░░░░░] = built ratio per feature

---

## FEATURE 1 — Tenant Registration and Onboarding
Progress: [████████░░] 80%

### Built
- ✅ POST api/auth/register/admin — RegisterCommandHandler creates Tenant + Admin user
- ✅ GET api/auth/verify-account — AccountVerificationCommandHandler
- ✅ GET api/settings/tenant — GetTenantSettingsQueryHandler
- ✅ PATCH api/settings/tenant — UpdateTenantSettingsHandler
- ✅ Tenant entity with status management (Active, Suspended, Deactivated)
- ✅ TenantProfile entity with timezone, currency, locale, address
- ✅ TenantSettings entity with booking confirmation mode, cancellation policy, notification flags
- ✅ POST api/settings/payment-gateway — ConfigurePaymentGatewayHandler

### Gaps
- ❌ Slug sanitization broken: RegisterCommandHandler derives slug from OrganizationName.ToLowerInvariant() — no replacement of spaces, so "City Med" produces "city med" (with space) which breaks the slug format constraint. Fix: replace spaces with hyphens and strip non-alphanumeric characters before slug creation.
- ❌ Orphaned tenant on user creation failure: RegisterCommandHandler calls CompleteAsync() to save Tenant before creating the User. If CreateAsync(user) fails, the tenant is left in the database with no admin. Both operations must run inside a single atomic transaction.
- ❌ No Super Admin onboarding approval/rejection flow (UC-1.1.1 Alternate Flow C): no endpoint to list pending tenant requests, approve, or reject with reason.
- ❌ No Super Admin Tenant Management endpoints: no endpoint for Super Admin to list all tenants or change tenant status (suspend, reactivate, deactivate).
- 🟡 AccountVerificationCommandHandler: expiry check runs AFTER ConfirmEmailAsync has already written the email-confirmed state to the database — ordering bug. The expiry must be validated before any write.
- 🟡 AccountVerificationCommandHandler injects IUserContextService but never calls it — dead dependency.
- ❌ Logo upload and ThemeColor fields are not on TenantProfile entity or UpdateProfile. UC-1.1.2 requires both for customer-facing branding.
- ❌ No GET api/settings/profile endpoint to return TenantProfile data to the admin frontend.
- ❌ No PATCH api/settings/profile endpoint (update profile fields separately from settings).

---

## FEATURE 2 — User and Role Management
Progress: [██████░░░░] 60%

### Built
- ✅ POST api/auth/register/customer/{slug} — RegisterCustomerCommandHandler (tenant-scoped)
- ✅ Staff invitation flow in RegisterCommandHandler (creates with Invited status, sends email)
- ✅ UserRole enum: TenantAdmin, Manager, Staff, Customer
- ✅ UserStatus: Invited, Active, Inactive
- ✅ User entity with InvitationToken, InvitationExpiresAt, EmailConfirmationToken
- ✅ UserResourceAssignment entity and table
- ✅ Authorization policies: CustomerOnly, StaffAndAbove, ManagerAndAbove, AdminOnly, Authenticated
- ✅ IUserContextService: GetCurrentJti, GetUserRole, GetCurrentUserId, IsAuthenticated, GetCurrentTenantId

### Gaps
- ❌ No POST api/users endpoint — Tenant Admin cannot create a staff user through the API. Only admin registration (self-sign-up) is implemented. UC-2.1.1 is not implemented.
- ❌ No GET api/users endpoint — no staff user list.
- ❌ No PATCH api/users/{id}/role endpoint — Tenant Admin cannot change a user's role. UC-2.2.1 is not implemented.
- ❌ No PATCH api/users/{id}/status endpoint — Tenant Admin cannot deactivate a user.
- ❌ No GET api/users/{id} endpoint — no individual user detail view.
- ❌ No resend invitation endpoint.
- ❌ Accept invitation endpoint missing — staff users cannot complete their invitation flow (set password via token).
- 🟡 AuthController.ResetPassword has [Authorize] attribute — a user who forgot their password is unauthenticated and cannot hit this endpoint. Must be [AllowAnonymous]. Password reset is entirely broken.
- 🟡 ResetPasswordCommandHandler calls _userContext.GetCurrentUserId() — this fails for unauthenticated users. Must resolve user from the reset token instead.
- 🟡 ForgotPasswordCommandHandler returns Failure("Users doesn't exist") when user not found — email enumeration vulnerability. Must return the same success response regardless of whether the email exists.
- 🟡 AuthController.Refresh endpoint has no [AllowAnonymous] and no [Authorize] — request will fail depending on middleware order.
- 🟡 AuthController inherits from Controller instead of ControllerBase — Controller pulls in MVC view rendering that an API controller does not need.
- ❌ No customer portal authentication surface: no GET api/customer/profile, no way for an authenticated customer to view their own profile.
- ❌ UC-2.1.3 (Guest Booking Lookup): no GET api/public/booking/lookup?ref={}&email={} endpoint or handler.

---

## FEATURE 3 — Resource Management
Progress: [████████░░] 80%

### Built
- ✅ POST api/resource — CreateResourceHandler
- ✅ GET api/resource — GetResourcesQueryHandler (paginated)
- ✅ GET api/resource/{id} — GetResourceByIdQueryHandler
- ✅ PATCH api/resource/{id} — UpdateResourceCommandHandler
- ✅ PATCH api/resource/{id}/status — DeactivateResourceCommandHandler
- ✅ PUT api/resource/{id}/availability — SetResourceAvailabilityHandler (full weekly schedule replacement)
- ✅ GET api/resource/{id}/availability — GetResourceAvailabilityQueryHandler
- ✅ POST api/resource/{id}/exceptions — AddExceptionHandler
- ✅ DELETE api/resource/{id}/exceptions/{exceptionId} — RemoveExceptionHandler
- ✅ GET api/resource/{id}/exceptions — GetResourceExceptionsQueryHandler
- ✅ ResourceAvailabilitySchedule entity with break periods
- ✅ ResourceAvailabilityException entity with ExceptionType enum

### Gaps
- 🟡 CreateResourceHandler returns BaseResponse<Resource> — exposes the raw domain entity, not a DTO. Must return BaseResponse<ResourceResponseDto> or equivalent. This is a Severity 1 violation.
- 🟡 ResourceController all actions use bare [Authorize] with no policy — any authenticated user (including Customer) can create, update, or deactivate resources. Must use [Authorize(Policy="ManagerAndAbove")] on write endpoints.
- 🟡 PUT api/resource/{resourceId}/availability route is missing the :guid constraint on the resourceId segment — inconsistent with all other resource routes.
- 🟡 ResourceController contains business logic: it validates the ResourceType enum value before dispatching the command. Validation belongs in the command handler or a FluentValidation validator.
- 🟡 RemoveExceptionHandler throws BusinessRuleBrokenException instead of returning Failure() when exception not found — inconsistent with AddExceptionHandler which returns Failure.
- ❌ Service.Color field does not exist on the Service entity or database. UC-3.1.2 requires a color code per service for calendar display.
- ❌ No Color field on Resource entity (minor, may not be needed if only Service has color).

---

## FEATURE 4 — Service Management
Progress: [████████░░] 80%

### Built
- ✅ POST api/service — CreateServiceHandler
- ✅ GET api/service — GetServicesQueryHandler (paginated)
- ✅ GET api/service/{id} — GetServiceByIdQueryHandler
- ✅ PATCH api/service/{id} — UpdateServiceHandler
- ✅ PATCH api/service/{id}/status — DeactivateServiceHandler
- ✅ Service entity with duration, buffer, price, currency, advance booking rules
- ✅ ServiceResource join entity with ReplaceResources, AddResource, RemoveResource on aggregate

### Gaps
- 🟡 ServiceController all actions use bare [Authorize] with no policy — any authenticated user can modify services. Must apply ManagerAndAbove on write endpoints.
- 🟡 Service.Update() does not validate CurrencyCode — Service.Create() validates it but Update() does not. An invalid currency code can be written through the update path.
- ❌ Color field missing on Service entity and database. Required for calendar color-coding per UC-3.1.2 and UC-3.3.1.
- ❌ PATCH api/service/{id} does not include resource assignment replacement in the current implementation — UpdateServiceHandler needs to handle the optional resource_ids array replacement in a transaction.

---

## FEATURE 5 — Availability Scheduling
Progress: [█████████░] 90%

### Built
- ✅ PUT api/resource/{id}/availability (full schedule replacement)
- ✅ GET api/resource/{id}/availability
- ✅ POST api/resource/{id}/exceptions
- ✅ DELETE api/resource/{id}/exceptions/{exceptionId}
- ✅ ResourceAvailabilitySchedule with DayOfWeek, IsAvailable, StartTime, EndTime
- ✅ ResourceBreakPeriod with BreakStartTime, BreakEndTime, Label
- ✅ ResourceAvailabilityException with ExceptionType (UnavailableAllDay, UnavailableHours, AvailableExtraHours)
- ✅ SetResourceAvailabilityHandler correctly parses HH:mm strings (TR-15.2 compliant)

### Gaps
- 🟡 SetResourceAvailabilityCommand is `public record` without `sealed` — minor but inconsistent with the convention.
- 🟡 AddExceptionCommand has `TimeOnly? StartTime, TimeOnly? EndTime` fields — TR-15.2 violation. Time fields in commands must be strings (HH:mm). Domain conversion must happen inside the handler. Must change to `string? StartTime, string? EndTime`.

---

## FEATURE 6 — Booking Flow (Guest-First)
Progress: [███░░░░░░░] 30%

### Built
- ✅ POST api/booking (authenticated path only — guest path not implemented)
- ✅ GET api/booking — GetBookingsQueryHandler (paginated, tenant-scoped)
- ✅ GET api/booking/{id} — GetBookingByIdQueryHandler
- ✅ POST api/booking/{id}/cancel — CancelBookingCommandHandler
- ✅ Booking entity: BookingId, TenantId, BookingReference, ServiceId, ResourceId, UserId, ScheduledDate, ScheduledStartTime, ScheduledEndTime, DurationMinutes, Status, ConfirmationMode, PriceSnapshot, CurrencyCode, CustomerNotes, CancellationReason, CancelledAt, CancelledByUserId, RescheduledFromBookingId
- ✅ BookingStatus enum: PendingPayment, Pending, Confirmed, Cancelled, Completed, NoShow
- ✅ BookingStatusLog entity — status history appended on every state transition
- ✅ Booking.Create() derives ScheduledEndTime and sets initial status from priceSnapshot and confirmationMode
- ✅ SlotAvailabilityService domain service — computes slots from schedules, exceptions, breaks, active bookings, buffer times
- ✅ GET api/public/services/{serviceId}/slots — GetAvailableSlotsQueryHandler

### Gaps (Domain Layer)
- ❌ Booking entity missing guest fields: GuestName, GuestEmail, GuestPhone, GuestCancellationTokenHash, GuestCancellationTokenExpiry. Without these, guest booking cannot be stored.
- ❌ Booking.Create() currently requires non-empty UserId and throws InvalidOperationException for Guid.Empty — this blocks guest booking creation at the domain level. Must accept null UserId when guest fields are provided instead.
- ❌ No database migration adding guest columns. The InitialMigration (2026-05-09) does not include these columns.

### Gaps (Application Layer)
- ❌ CreateBookingCommand uses `TimeOnly ScheduledStartTime` — TR-15.2 violation. Must be `string ScheduledStartTime` with TimeOnly.ParseExact conversion in the handler.
- ❌ CreateBookingCommandHandler has no guest booking path — it always expects an authenticated UserId from IUserContextService. Must branch: if request contains GuestEmail, treat as guest; else read UserId from context.
- ❌ No guest cancellation token generation in CreateBookingCommandHandler.
- ❌ GetBookingByIdQueryHandler returns string.Empty for ServiceName and ResourceName — the handler does not project service and resource names into the response. BookingDetailDto fields are populated but empty.
- ❌ Booking reference generation uses count+1 inside a transaction — race condition exists. Two concurrent bookings created in the same millisecond can receive the same reference number. Must use a sequence or database-level row locking on the reference counter.
- ❌ No handler for UC-3.2.5: Guest cancellation via secure token. No endpoint POST api/public/booking/cancel exists.
- ❌ No handler for UC-3.2.6: Staff Creates Booking. No POST api/booking/staff endpoint.

### Gaps (Controller Layer)
- ❌ POST api/booking has [Authorize] — blocks all unauthenticated requests. Per TR-10.1 Path A, guest booking must not require authentication. The endpoint must be [AllowAnonymous] and the handler must determine whether the request is guest or authenticated by inspecting the request body.
- ❌ GET api/booking/customer/{customerId} exists but BookingController most actions use bare [Authorize] with no policy — customers can access the admin booking list endpoint, staff can access other tenants' data. Must apply correct policies per endpoint.
- ❌ No POST api/public/booking/cancel endpoint in PublicController.
- ❌ No GET api/public/booking/lookup endpoint in PublicController.
- ❌ No POST api/booking/{id}/staff endpoint for staff-created bookings.
- ❌ CreatedAtAction in BookingController accesses result.Data.BookingId without null-checking result.Data — will throw NullReferenceException if Data is null.

### Gaps (Public Endpoints)
- ❌ GET api/public/{slug}/services/{serviceId}/resources — exists but returns PublicResourceDto. The staff selection step requires: staff name, description, and their weekly availability schedule summary (days and hours) so the customer can view info before selecting. The current PublicResourceDto likely does not include availability data.
- ❌ GET api/public/services/{serviceId}/slots currently accepts slug as a query parameter (inconsistent with the rest of the public controller which uses slug as a path segment). Minor inconsistency — review and standardise.

---

## FEATURE 7 — Admin Calendar View
Progress: [░░░░░░░░░░] 0%

### Built
- Nothing.

### Gaps
- ❌ No CalendarController.
- ❌ No GET api/calendar endpoint.
- ❌ No GetCalendarQuery or GetCalendarQueryHandler.
- ❌ No calendar DTO (CalendarEventDto with bookingReference, serviceName, serviceColor, resourceName, customerName, startTime, endTime, status).
- ❌ No IBookingRepository method to fetch bookings within a date range for calendar rendering.
- ❌ No resource availability window projection for the calendar view (TR-12.2 requires availability windows to be returned alongside bookings).

---

## FEATURE 8 — Payment Collection
Progress: [████░░░░░░] 40%

### Built
- ✅ TenantPaymentGateway entity with GatewayProvider, PublishableKey, SecretKeyEncrypted, WebhookId, Mode, IsActive
- ✅ POST api/settings/payment-gateway — ConfigurePaymentGatewayHandler (saves credentials)
- ✅ GET api/settings/payment-gateway — GetPayPalSettingsHandler
- ✅ POST api/booking/{id}/payment — InitiatePaymentHandler (creates PayPal order)
- ✅ POST api/webhooks/payment/paypal — HandlePayPalWebhookHandler with PayPalWebhookValidator (signature validation present)
- ✅ PaymentTransaction entity with Status, Amount, CurrencyCode, GatewayTransactionId, PaidAt
- ✅ PaymentTransactionRepository with GetByBookingIdAsync, GetByGatewayTransactionIdAsync

### Gaps
- ❌ No Refund entity or RefundRepository. When a cancelled paid booking triggers refund eligibility, there is no record to store. The database has no refunds table.
- ❌ No refund handler (POST api/payment/refund/{bookingId}) — Tenant Admin cannot initiate a gateway refund.
- ❌ No refund status tracking or refund queue view.
- 🟡 InitiatePaymentHandler creates HttpClient via `new HttpClient()` in three separate private static methods — socket exhaustion risk under load. Must inject IHttpClientFactory and use named clients.
- 🟡 POST api/booking/{id}/payment uses [Authorize(Policy="CustomerOnly")] — this blocks guest payment initiation since guests are unauthenticated. If guest booking is to support paid services, the payment initiation flow must support an unauthenticated path or use a different trigger (e.g., redirect-based PayPal flow tied to booking reference).
- ❌ Payment gateway credential validation (test request after save) is not implemented in ConfigurePaymentGatewayHandler — credentials are stored without validation.
- ❌ No GET api/payment endpoint for Tenant Admin to view payment transaction history.

---

## FEATURE 9 — Email Notifications
Progress: [█░░░░░░░░░] 10%

### Built
- ✅ BrevoSmtpService exists in Infrastructure — sends emails via Brevo SMTP.
- ✅ EmailSettings configuration class with SMTP host, port, credentials.
- ✅ ForgotPasswordCommandHandler sends password reset email.
- ✅ RegisterCommandHandler sends email verification link.

### Gaps
- ❌ No INotificationService interface — no contract for dispatching booking-related notifications.
- ❌ No notification dispatch in CreateBookingCommandHandler — booking confirmation email is never sent after booking creation.
- ❌ No notification dispatch in CancelBookingCommandHandler — cancellation email is never sent.
- ❌ No email template system — all notification types (booking confirmed, pending, cancelled, rescheduled, reminder, refund processed) require templates that embed tenant branding (business name, logo, theme color) and booking details.
- ❌ No guest booking confirmation email with embedded cancellation link.
- ❌ No staff alert email when new booking is placed.
- ❌ No booking reminder system — requires a background job scheduler (Hangfire or IHostedService) that queries upcoming bookings and dispatches reminders at the tenant-configured lead time.
- ❌ No notification_logs table or entity to track notification delivery status and retries.
- ❌ notification_logs DbSet not present in ApexBookingDbContext.

---

## FEATURE 10 — Audit Logging
Progress: [░░░░░░░░░░] 0%

### Built
- Nothing.

### Gaps
- ❌ No AuditLog entity.
- ❌ No audit_logs table (not in DbContext, not in any migration).
- ❌ No IAuditService interface or implementation.
- ❌ No audit log writes anywhere in the application — no handler or middleware records audit events.
- ❌ No GET api/audit endpoint for Tenant Admin or Super Admin.
- ❌ Required audit events not recorded: booking.created, booking.cancelled, booking.rescheduled, booking.confirmed, booking.rejected, user.created, user.invited, user.role.changed, user.deactivated, tenant.registered, tenant.status.changed, tenant.settings.updated, payment.captured, refund.initiated, refund.processed, refund.failed.

---

## FEATURE 11 — Booking Rescheduling
Progress: [░░░░░░░░░░] 0%

### Built
- ✅ RescheduledFromBookingId field exists on Booking entity (database column exists in InitialMigration).

### Gaps
- ❌ No RescheduleBookingCommand or RescheduleBookingCommandHandler.
- ❌ No PATCH api/booking/{id}/reschedule endpoint.
- ❌ No reschedule confirmation email dispatch.
- ❌ Booking entity has no Reschedule() method — the domain does not enforce rescheduling rules (only cancellable bookings can be rescheduled, slot must be available, original slot must be freed atomically).

---

## FEATURE 12 — Analytics Dashboard
Progress: [░░░░░░░░░░] 0%

### Built
- Nothing.

### Gaps
- ❌ No analytics controller.
- ❌ No GET api/analytics/dashboard endpoint.
- ❌ No GetDashboardQuery or GetDashboardQueryHandler.
- ❌ No DTO for analytics response (total revenue, total bookings, bookings by status, revenue by service, booking trend, today's schedule).
- ❌ No IBookingRepository method optimized for analytics aggregation (sum by service, count by status, daily trend grouping).

---

## FEATURE 13 — Client Directory
Progress: [░░░░░░░░░░] 0%

### Built
- Nothing.

### Gaps
- ❌ No client directory controller.
- ❌ No GET api/clients endpoint.
- ❌ No GET api/clients/{email}/bookings endpoint.
- ❌ No GetClientsQuery or GetClientsQueryHandler.
- ❌ No ClientSummaryDto (name, email, phone, totalBookings, totalSpent, lastVisitDate).
- ❌ No IBookingRepository method that groups bookings by guest email or userId to produce the client list.
- ❌ Client records must merge guest bookings (by GuestEmail) and authenticated customer bookings (by UserId → User.Email) into a unified view. This requires the guest fields on Booking to exist first (Feature 6 gap).

---

## DOMAIN-LEVEL GAPS (cut across multiple features)

These gaps block multiple features and must be resolved before the dependent features can be implemented.

| Gap | Blocks | Priority |
|-----|--------|----------|
| Booking entity missing GuestName, GuestEmail, GuestPhone, GuestCancellationTokenHash, GuestCancellationTokenExpiry | Feature 6 (guest booking), Feature 9 (guest email), Feature 13 (client directory) | Critical |
| Booking.Create() throws for null/empty UserId | Feature 6 (guest booking) | Critical |
| No database migration for guest fields | Feature 6 | Critical |
| Service entity missing Color field | Feature 4, Feature 7 (calendar color-coding) | High |
| No database migration for Service.Color | Feature 4, Feature 7 | High |
| No Refund entity or table | Feature 8 (refunds) | High |
| No AuditLog entity or table | Feature 10 | High |
| No notification_logs entity or table | Feature 9 | Medium |
| TenantProfile missing LogoUrl and ThemeColor fields | Feature 1 (branding), Feature 6 (public page branding) | Medium |

---

## CRITICAL BUGS (blocking correctness regardless of feature completeness)

These are existing code defects that break already-built functionality and must be fixed.

| ID | Severity | Location | Description |
|----|----------|----------|-------------|
| BUG-01 | Critical | ApexBookingDbContext.BuildTenantFilter | Returns null when _tenantService.TenantId is null at OnModelCreating time (startup). HasQueryFilter(null) is called, meaning the global tenant isolation filter is never applied. All queries return cross-tenant data. Row-level isolation is broken. |
| BUG-02 | Critical | AuthController.ResetPassword | Has [Authorize] attribute. An unauthenticated user with a forgot-password token cannot reach this endpoint. Password reset is entirely non-functional. |
| BUG-03 | Critical | ResetPasswordCommandHandler | Calls _userContext.GetCurrentUserId() to find the user — requires an active JWT. Must resolve the user from the reset token instead. |
| BUG-04 | Critical | RegisterCommandHandler | Slug derived from OrganizationName.ToLowerInvariant() with no space or special character handling. Org name "City Med" produces slug "city med" containing a space, which violates the slug format constraint and breaks all tenant routing. |
| BUG-05 | Critical | ForgotPasswordCommandHandler | Returns Failure("Users doesn't exist") when email is not found. This is an email enumeration vulnerability — attackers can probe which emails are registered. Must return success regardless. |
| BUG-06 | Critical | CreateBookingCommand | ScheduledStartTime field is typed as TimeOnly — TR-15.2 violation. DTOs and command records must hold string (HH:mm). The binding is broken for any client sending a string time field. |
| BUG-07 | Critical | AddExceptionCommand | StartTime and EndTime fields are typed as TimeOnly? — TR-15.2 violation. Must be string?. |
| BUG-08 | Critical | CreateResourceHandler | Returns BaseResponse<Resource> — the raw domain entity. Exposes internal domain model to the API surface. Must return a DTO. |
| BUG-09 | High | AccountVerificationCommandHandler | Expiry check runs AFTER ConfirmEmailAsync has already written the verified state to the database. A token that is expired still verifies the email. Order must be: validate expiry first, then confirm. |
| BUG-10 | High | BookingController | Most actions decorated with bare [Authorize] and no policy. Customers can call the admin booking list endpoint. Staff can access bookings from other resources. Policy must be declared per endpoint. |
| BUG-11 | High | CreateBookingCommandHandler | Booking reference generated by counting existing bookings and adding 1 inside the transaction. Two concurrent requests in the same millisecond receive the same count and produce a duplicate reference. Must use a locking strategy or a DB sequence. |
| BUG-12 | High | GetBookingByIdQueryHandler | Returns string.Empty for ServiceName and ResourceName. BookingDetailDto fields are populated empty — the handler does not join service and resource data. |
| BUG-13 | High | ResourceController | Contains enum validation logic (ResourceType range check) in the controller body. Validation must be in a command validator, not the controller. |
| BUG-14 | High | RemoveExceptionHandler | Throws BusinessRuleBrokenException when the exception is not found instead of returning Failure(). Breaks the standard ICommandHandler error handling contract and causes an unhandled exception response. |
| BUG-15 | High | InitiatePaymentHandler | Creates HttpClient via `new HttpClient()` in three private static methods. Under concurrent load this exhausts socket connections. Must inject IHttpClientFactory. |
| BUG-16 | Medium | AuthController.Refresh | No [AllowAnonymous] attribute. The refresh endpoint must be accessible without a valid access token (the point of refresh is that the access token is expired). |
| BUG-17 | Medium | AuthController | Inherits from Controller instead of ControllerBase. Controller pulls in MVC view rendering dependencies not needed for an API controller. |
| BUG-18 | Medium | RegisterCommandHandler | Commits Tenant to the database before User creation. If user creation fails, an orphaned Tenant record remains. Must wrap both operations in a single atomic transaction. |
| BUG-19 | Medium | BookingController.Create | Accesses result.Data.BookingId in the CreatedAtAction call without null-checking result.Data. Will throw NullReferenceException if the handler returns a successful result with no Data. |
| BUG-20 | Medium | GetAvailableSlotsQueryHandler | Does not validate that the requested serviceId belongs to the same tenant as the slug. A request can query slots for a service from a different tenant using a valid slug. |
| BUG-21 | Medium | Service.Update() | Does not validate CurrencyCode. Service.Create() validates it but Update() does not — an invalid currency can be written through the update path. |
| BUG-22 | Medium | AccountVerificationCommandHandler | Injects IUserContextService but never calls it. Dead dependency adds confusion and should be removed. |

---

## IMPLEMENTATION ORDER (dependency-safe sequence)

Based on the dependency map in mvp.md and the gaps above, the safe implementation order is:

### Phase 1 — Fix Critical Bugs (no new features blocked until these are resolved)
1. BUG-01: Fix BuildTenantFilter in ApexBookingDbContext — use closure-based expression so the filter evaluates per-query
2. BUG-04: Fix slug sanitization in RegisterCommandHandler
3. BUG-18: Wrap tenant + user creation in a single transaction
4. BUG-06: Fix CreateBookingCommand.ScheduledStartTime to string
5. BUG-07: Fix AddExceptionCommand.StartTime/EndTime to string?
6. BUG-08: Fix CreateResourceHandler return type to DTO
7. BUG-02 + BUG-03: Fix ResetPassword — remove [Authorize], resolve user from token in handler
8. BUG-05: Fix ForgotPassword email enumeration

### Phase 2 — Domain Model Extensions (must precede all dependent features)
1. Add GuestName, GuestEmail, GuestPhone, GuestCancellationTokenHash, GuestCancellationTokenExpiry to Booking entity
2. Update Booking.Create() to accept null UserId when guest fields are provided
3. Add Color field to Service entity
4. Add LogoUrl and ThemeColor to TenantProfile entity
5. Add Refund entity
6. Add AuditLog entity
7. Add NotificationLog entity
8. Create migration covering all of the above

### Phase 3 — User Management (enables staff workflows)
1. POST api/users (Tenant Admin creates staff)
2. GET api/users (staff list)
3. PATCH api/users/{id}/role
4. PATCH api/users/{id}/status
5. Accept invitation endpoint (staff completes invitation)
6. Fix BUG-16 (Refresh [AllowAnonymous])
7. Fix BUG-17 (AuthController inherits ControllerBase)

### Phase 4 — Guest Booking Flow (core product loop)
1. Update CreateBookingCommandHandler — guest path (no auth required) and authenticated path
2. Update POST api/booking — remove [Authorize], detect guest vs authenticated from request
3. Add guest cancellation token generation in handler
4. Add POST api/public/booking/cancel handler and endpoint
5. Add GET api/public/booking/lookup handler and endpoint
6. Add POST api/booking/staff handler and endpoint (staff-created bookings)
7. Fix BUG-11 (booking reference race condition)
8. Fix BUG-12 (GetBookingByIdQueryHandler ServiceName/ResourceName)
9. Fix BUG-19 (CreatedAtAction null check)
10. Fix BUG-10 (BookingController policies)
11. Fix BUG-14 (RemoveExceptionHandler exception vs Failure)
12. Fix BUG-20 (GetAvailableSlotsQueryHandler tenant validation)
13. Fix BUG-13 (ResourceController enum validation in controller)
14. Fix BUG-09 (AccountVerification expiry check order)

### Phase 5 — Notifications (depends on guest booking being complete)
1. Define INotificationService interface
2. Implement booking notification templates (embedding tenant logo, color, contact)
3. Wire notification dispatch in CreateBookingCommandHandler (guest confirmation with cancellation link)
4. Wire notification dispatch in CancelBookingCommandHandler
5. Add booking reminder background job
6. Add notification_logs write on every dispatch
7. Fix BUG-15 (IHttpClientFactory in InitiatePaymentHandler)

### Phase 6 — Payments and Refunds
1. Add Refund entity and RefundRepository
2. Wire refund record creation in CancelBookingCommandHandler when paid booking is cancelled
3. Add POST api/payment/refund/{bookingId} handler and endpoint
4. Add GET api/payment (payment history for admin)
5. Add payment gateway credential validation in ConfigurePaymentGatewayHandler
6. Fix guest payment initiation flow (PayPal redirect-based for unauthenticated users)

### Phase 7 — Admin Dashboard Features (depends on bookings being complete)
1. Booking Rescheduling: RescheduleBookingCommand, handler, PATCH api/booking/{id}/reschedule
2. Calendar View: GetCalendarQuery, handler, GET api/calendar
3. Analytics Dashboard: GetDashboardQuery, handler, GET api/analytics/dashboard
4. Client Directory: GetClientsQuery, handler, GET api/clients, GET api/clients/{email}/bookings

### Phase 8 — Audit Logging
1. AuditLog entity, IAuditService interface, implementation
2. Wire audit writes after every state-changing operation (post-command, via pipeline behavior or direct call)
3. GET api/audit endpoint for Tenant Admin

### Phase 9 — Tenant Administration (Super Admin)
1. Super Admin onboarding approval/rejection flow
2. GET api/admin/tenants (list all tenants)
3. PATCH api/admin/tenants/{id}/status (suspend, reactivate, deactivate)
