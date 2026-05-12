# Complete Feature Specification: Multitenant Booking System

## SCOPE

This document defines the complete feature set and technical requirements for the ApexBooking platform. It is no longer scoped to a minimum viable product. All features described here are planned for full implementation. The "Status" field on each feature tracks current build state.

The core loop this system must deliver: a business signs up, configures their services and staff, and accepts bookings from customers. Customers book without creating an account. Staff manage their schedule. Admins see the full picture.

---

## DEPENDENCY MAP

Before defining scope, every feature's dependencies must be clear. A feature cannot be built before its dependency is built.

### Dependency Chain (bottom to top)

Level 0 (no dependencies, build first): Tenant Registration, Subscription Plans (platform table, seeded data), Super Admin account (seeded).

Level 1 (depends on Level 0): Tenant Profile Configuration, Tenant Settings Configuration, User and Role Management.

Level 2 (depends on Level 1): Resource Management, Location Management.

Level 3 (depends on Level 2): Service Management, Resource Availability Schedules.

Level 4 (depends on Level 3): Booking Flow (guest-first customer-facing), Admin Calendar View.

Level 5 (depends on Level 4): Payment Collection, Notifications, Audit Logging.

Level 6 (depends on Level 5): Refunds, Rescheduling, Cancellation.

Level 7 (depends on Level 4): Analytics Dashboard, Client Directory.

### Dependent Features

Booking Flow depends on Services, Resources, Availability Schedules. Guest booking does not depend on User Accounts. Payment Collection depends on Booking Flow and Payment Gateway Configuration. Refunds depend on Payment Collection and Cancellation. Cancellation depends on Booking Flow. Rescheduling depends on Booking Flow and Availability Schedules. Notifications depend on Booking Flow. Audit Logging depends on Users, Bookings, Tenants. Analytics Dashboard depends on Bookings. Client Directory depends on Bookings. Tenant Subscription Billing depends on Subscription Plans and Tenant Registration. Role Enforcement depends on User Accounts and Role Definitions.

---

## FEATURES

### FEATURE 1: Tenant Registration and Onboarding

Included from use cases: UC-1.1.1, UC-1.1.2, UC-1.1.3 (status management, Super Admin only), UC-1.1.4.

Reason for inclusion: Without this, no tenant exists. This is the entry point of the entire system.

Dependencies: None. This is Level 0.

Dependent features: Everything else in the system.

Status: Built.

---

### FEATURE 2: User and Role Management

Included from use cases: UC-2.1.1, UC-2.1.2, UC-2.2.1, UC-2.2.2.

Note: Customer account registration (UC-2.1.2) is optional and post-booking. Customers are not required to register to make a booking. UC-2.1.3 (guest booking lookup) is served by the public booking surface with no authentication.

Reason for inclusion: Without staff users and roles, there is no authentication boundary, no staff assignment, and no permission enforcement.

Dependencies: Tenant Registration.

Dependent features: Booking Flow (staff-side), Calendar View, Analytics Dashboard.

Status: Built.

---

### FEATURE 3: Resource Management

Included from use cases: UC-3.1.1, UC-3.1.3, UC-3.1.4.

Reason for inclusion: Resources are what get booked. Without them, no booking slot can be generated.

Dependencies: Tenant Registration, User Accounts.

Dependent features: Service Management, Booking Flow, Calendar View.

Status: Built. The availability schedule save bug (duplicate key on re-save) was fixed by switching SetResourceAvailabilityHandler to use FindByIdWithAvailabilityAsync so EF Core tracks existing child rows correctly.

---

### FEATURE 4: Service Management

Included from use cases: UC-3.1.2.

Reason for inclusion: Services define what customers are booking, how long it takes, how much it costs, and which staff deliver it.

Dependencies: Resource Management.

Dependent features: Booking Flow.

Status: Built (backend and frontend).

---

### FEATURE 5: Availability Scheduling

Included from use cases: UC-3.1.3, UC-3.1.4.

Reason for inclusion: Without availability rules, the system cannot compute open booking slots.

Dependencies: Resource Management.

Dependent features: Booking Flow.

Status: Built as part of Resource feature (TR-7.4, TR-7.5, TR-7.6). A GET endpoint for reading the saved schedule was added: GET api/resource/{resourceId}/availability. This returns ResourceAvailabilityDto with the full schedule including breaks. The frontend ResourceAvailabilityPage calls this on mount to populate the form with the saved state.

---

### FEATURE 6: Booking Flow (Guest-First)

Included from use cases: UC-3.2.1, UC-3.2.2, UC-3.2.4, UC-3.2.5, UC-3.2.6.

Reason for inclusion: This is the core product. The booking flow is guest-first by design. No account is required at any point during the customer booking wizard.

The booking wizard follows this 5-step sequence:
Step 1: Service selection (name, description, duration, price shown per service).
Step 2: Date and time selection (calendar and time slots on one screen, slots computed dynamically).
Step 3: Staff selection (only staff available for the selected service and time are shown; Any Available option always present).
Step 4: Contact details (full name, email, phone — required; notes — optional; no login prompt).
Step 5: Confirm (success screen with booking summary, "Book Another" button, confirmation email sent immediately).

Guest cancellation is handled via a secure token link included in the confirmation email (UC-3.2.5). No account is required to cancel.

Staff-created bookings (UC-3.2.6) allow admins and staff to book on behalf of walk-in or phone customers from the admin dashboard.

Dependencies: Services, Resources, Availability Schedules.

Dependent features: Payment Collection, Notifications, Audit Logging.

Status: Backend fully built. Tenant Admin frontend fully built. Guest-first customer wizard: in progress. Guest cancellation token endpoint: not built. Staff-created booking from admin: not built.

#### Public Booking Surface

The public booking URL per tenant is /book/{tenant-slug}. The platform does not expose a public directory of all tenants. Each tenant owns their customer relationship and shares their booking URL directly with customers (website, printed card, social link).

The public booking surface consists of:

/book/:tenant — tenant landing page. Fetches tenant profile and branding from GET api/public/{slug}. Fetches active service catalog from GET api/public/{slug}/services. Renders business name, logo, theme color, address, and list of services. No authentication required.

/book/:tenant/new — 5-step booking wizard. Fetches resources per service from GET api/public/{slug}/services/{serviceId}/resources. Fetches available slots from GET api/public/services/{serviceId}/slots?resourceId={guid}&date={date}&slug={slug}. Submits booking to POST api/booking with guest contact details. No authentication required at any step.

/book/:tenant/cancel?token={raw-token} — guest cancellation page. Accepts raw cancellation token, validates it against the hashed stored value, and allows the guest to cancel without logging in.

/book/:tenant/lookup — guest booking lookup. Accepts booking reference and email, returns booking details, and triggers a cancellation email link if the booking is cancellable.

All public endpoints live in PublicController under [Route("api/[controller]")] resolving to api/public. All actions use [AllowAnonymous]. Handlers for public endpoints do not call IUserContextService. They resolve tenant by slug using ITenantRepository.FindBySlugAsync(slug). Repository methods called from public handlers use IgnoreQueryFilters() and apply TenantId explicitly via a Where clause.

---

### FEATURE 7: Admin Calendar View

Included from use cases: UC-3.3.1.

Reason for inclusion: Staff and admins need to see their schedule. Color-coded by service. Day, week, and month views. Clicking a booking block opens a detail popup.

Dependencies: Booking Flow.

Dependent features: None.

Status: Not built.

---

### FEATURE 8: Payment Collection

Included from use cases: UC-4.1.1, UC-4.3.1.

Reason for inclusion: If services have a price, the system must collect payment before confirming the booking.

Note: Refunds (UC-4.1.2) are included. When a paid booking is cancelled and the policy permits a refund, the system creates a refund record and initiates the gateway refund call.

Dependencies: Booking Flow, Payment Gateway Configuration.

Dependent features: Refunds.

Status: Not built.

---

### FEATURE 9: Email Notifications

Included from use cases: UC-5.1.1.

All notification types are included:
- Booking Confirmed (Guest): includes secure cancellation link.
- Booking Confirmed (Authenticated Customer): includes portal link.
- Booking Pending (awaiting manual confirmation).
- Booking Confirmed by Admin (manual mode): for guests, includes cancellation link.
- Booking Rejected by Admin.
- Booking Cancelled by Customer or Guest.
- Booking Cancelled by Staff or Admin.
- Booking Rescheduled: for guests, includes updated cancellation link.
- Booking Reminder (24 hours before, timing configurable): for guests, includes cancellation link.
- New Booking Alert to Staff and Admin.
- Refund Processed.

All templates use the tenant's business name, logo, theme color, and contact details.

Dependencies: Booking Flow.

Dependent features: None.

Status: Not built.

---

### FEATURE 10: Audit Logging

Included from use cases: UC-5.2.1.

Included events: booking.created, booking.cancelled, booking.rescheduled, booking.confirmed, booking.rejected, user.created, user.role.changed, tenant.status.changed, payment.captured, refund.processed.

Guest actions are recorded with actor set to "guest" and the booking ID as the affected record.

Reason for inclusion: Without an audit trail, debugging production issues is blind. Retrofitting this later is expensive.

Dependencies: Users, Bookings, Tenants.

Dependent features: None.

Status: Not built.

---

### FEATURE 11: Booking Rescheduling

Included from use cases: UC-3.2.3.

Reason for inclusion: Rescheduling is a standard expectation in any booking system. Authenticated customers and staff can reschedule through the portal or admin dashboard. Guest customers contact the business directly; staff reschedule on their behalf.

Dependencies: Booking Flow, Availability Schedules.

Dependent features: None.

Status: Not built.

---

### FEATURE 12: Analytics Dashboard

Included from use cases: UC-3.3.2.

Reason for inclusion: Admins and managers need revenue metrics, booking trends, and today's schedule at a glance. This is a core feature of any booking SaaS, not a scale concern.

Included metrics: total revenue, total bookings, bookings by status, revenue breakdown by service (chart), booking trend over time (chart), today's upcoming schedule.

Dependencies: Booking Flow.

Dependent features: None.

Status: Not built.

---

### FEATURE 13: Client Directory

Included from use cases: UC-3.3.3.

Reason for inclusion: Admins and managers need a lightweight client record: who has booked, how many times, how much they have spent, when they last visited. This is standard in every appointment-based booking SaaS.

Client records are not created manually. They are derived from booking records. Every unique email that has placed a booking under the tenant appears in the directory.

Dependencies: Booking Flow.

Dependent features: None.

Status: Not built.

---

## SCALE BACKLOG

Features that require additional infrastructure or are not yet prioritized.

### SCALE FEATURE S-1: Tenant Subscription and Platform Billing

Excluded use cases: UC-4.2.1, UC-4.2.2, UC-4.2.3.

Reason deferred: Billing infrastructure (Stripe Billing or similar) adds complexity. All tenants get full access during the current phase. Manual billing covers the interim period.

### SCALE FEATURE S-2: Multi-Location Support

Excluded from current scope entirely.

Reason deferred: Most early-stage tenants operate from one location. The locations table is in the schema but not exposed in the UI.

### SCALE FEATURE S-3: Full Audit Log UI for Tenant Admin

Partially deferred.

Reason deferred: Audit logs are written to the database. The Tenant Admin UI to read and export them is deferred.

### SCALE FEATURE S-4: Manual Confirmation Mode UI

Partially deferred.

Reason deferred: The BookingConfirmationMode column exists on the domain entity and the Pending status is fully supported. The admin approval workflow UI (approve/reject pending bookings with notification dispatch) is deferred.

---

## DETAILED TECHNICAL REQUIREMENTS

### TECHNICAL REQUIREMENT 1: Platform and Architecture

TR-1.1: The system must be built as a multi-tenant SaaS application where all tenants share a single database instance with row-level tenant isolation enforced by tenant_id on every query.

TR-1.2: The backend must expose a RESTful JSON API. Success responses for single resources use BaseResponse<T>. Paginated list responses use PagedResult<T> directly (never wrap PagedResult inside BaseResponse). Error responses use the standard error envelope.

TR-1.3: Every API request to a protected endpoint must pass through two middleware layers in order: authentication middleware (validates session token), then tenant scope middleware (injects tenant_id into the request context).

TR-1.4: The frontend must be a single-page application. Two distinct UI surfaces exist: the Tenant Admin dashboard (authenticated staff/admin) and the Public Booking Surface (no authentication required).

TR-1.5: The system must support HTTPS only.

TR-1.6: All environment-specific secrets must be stored in environment variables and never committed to source control.

---

### TECHNICAL REQUIREMENT 2: Database

TR-2.1: The following tables must exist: tenants, tenant_profiles, tenant_settings, tenant_payment_gateways, users, user_profiles, user_resource_assignments, password_reset_tokens, resources, resource_availability_schedules, resource_break_periods, resource_availability_exceptions, services, service_resources, bookings, booking_status_logs, payment_transactions, refunds, notification_logs, audit_logs, super_admins, subscription_plans (seeded), locations (table exists, not exposed in UI).

TR-2.2: The bookings table must include the following guest booking columns: guest_name (string, nullable), guest_email (string, nullable), guest_phone (string, nullable), guest_cancellation_token_hash (string, nullable), guest_cancellation_token_expiry (datetime, nullable). These columns are null when the booking is linked to a registered customer UserId. They are required when UserId is null.

TR-2.3: All primary keys must be UUIDs generated by the application layer, not the database.

TR-2.4: Every table must have created_at and updated_at timestamps.

TR-2.5: The following indexes must exist. bookings: (tenant_id, resource_id, scheduled_date, status). bookings: (tenant_id, user_id). bookings: (tenant_id, guest_email). bookings: (tenant_id, scheduled_date). bookings: (guest_cancellation_token_hash) partial where guest_cancellation_token_hash is not null. users: (tenant_id, email). audit_logs: (tenant_id, created_at). resource_availability_schedules: (resource_id, day_of_week).

TR-2.6: Database migrations must be versioned and run in order.

---

### TECHNICAL REQUIREMENT 3: Authentication and Session Management

TR-3.1: Authentication must use short-lived JWT access tokens (15-minute expiry) and long-lived refresh tokens (7-day expiry) stored in HTTP-only cookies.

TR-3.2: Login must accept email and password. The access token must contain: user_id, tenant_id, and role.

TR-3.3: On access token expiry, the client must automatically call the refresh endpoint using the refresh token cookie to obtain a new access token without requiring re-login.

TR-3.4: Password hashing must use bcrypt with a minimum cost factor of 12.

TR-3.5: Password reset flow: user submits email, system generates a cryptographically random token stored hashed in password_reset_tokens with 1-hour expiry, system sends reset link, user submits new password with raw token, system validates, updates password, marks token used.

TR-3.6: Invitation flow for staff users: Tenant Admin creates user via POST api/users, system creates user with status invited, sends invitation email with raw token, user sets password via accept invitation endpoint, system validates token, checks 72-hour expiry, sets password, sets status to active.

TR-3.7: Super Admin login must use a separate endpoint and a separate token that does not carry a tenant_id.

TR-3.8: The guest cancellation token must be generated using a cryptographically random source (minimum 32 bytes), stored as a SHA-256 hash in the bookings table, and transmitted raw only in the confirmation email. The token must be single-use and expire per the tenant's cancellation cutoff window.

---

### TECHNICAL REQUIREMENT 4: Role-Based Access Control

TR-4.1: Every protected API endpoint must declare the minimum role required to access it via an authorization policy.

TR-4.2: Role hierarchy (highest to lowest): tenant_admin, manager, staff, customer.

TR-4.3: Staff users accessing booking data must be filtered to only return bookings associated with resources assigned to them via user_resource_assignments.

TR-4.4: Customer users must only retrieve and modify their own booking records. Any attempt to access another customer's booking must return 403 Forbidden.

TR-4.5: No endpoint may return data belonging to a different tenant than the one in the authenticated user's token. This must be enforced in the application layer, not only in the controller layer.

---

### TECHNICAL REQUIREMENT 5: Tenant Management

TR-5.1: POST api/auth/register. Creates tenant + admin user in a single atomic transaction. Slug is sanitized from business name (lowercase, spaces replaced with hyphens, non-alphanumeric characters stripped). If the derived slug is taken, append a numeric suffix. Tenant is created before user; if user creation fails, the tenant must be rolled back.

TR-5.2: Email verification is required before the tenant admin can access the dashboard.

TR-5.3: Super Admin can set tenant status to active, suspended, or deactivated via PATCH api/admin/tenants/{id}/status.

TR-5.4: PATCH api/settings/tenant. Updates tenant profile fields. Timezone must be a valid IANA string. Currency must be a valid ISO 4217 code.

TR-5.5: PATCH api/settings/preferences. Updates tenant-level booking settings.

---

### TECHNICAL REQUIREMENT 6: User Management

TR-6.1: POST api/users. Creates staff user. Sends invitation email.

TR-6.2: Customer registration is optional and self-service via POST api/auth/register-customer scoped to a tenant slug.

TR-6.3: PATCH api/users/{id}/role. Updates user role. Accessible by tenant_admin only.

TR-6.4: PATCH api/users/{id}/status. Deactivates user.

TR-6.5: GET api/users. Lists users scoped to tenant. Supports pagination.

---

### TECHNICAL REQUIREMENT 7: Resource Management

TR-7.1: POST api/resource. Create resource. Accessible by tenant_admin and manager.

TR-7.2: PATCH api/resource/{id}. Update name, description, capacity. ResourceType is frozen after creation and cannot be changed.

TR-7.3: PATCH api/resource/{id}/status. Deactivate resource. Sets is_active to false.

TR-7.4: PUT api/resource/{id}/availability. Full replacement of weekly schedule. Parses HH:mm time strings in the handler using TimeOnly.ParseExact. Deletes and re-inserts all child schedule and break period rows in a single tracked EF Core operation.

TR-7.4a: GET api/resource/{id}/availability. Returns the current saved schedule as ResourceAvailabilityDto including all days and break periods. Used by the frontend to pre-populate the availability form on load.

TR-7.5: POST api/resource/{id}/exceptions. Add date exception. Exception date must be in the future. Duplicate date and type combination returns 409.

TR-7.6: DELETE api/resource/{id}/exceptions/{exceptionId}. Remove exception.

---

### TECHNICAL REQUIREMENT 8: Service Management

TR-8.1: POST api/service. Create service with resource_ids array (minimum 1). Includes color field for calendar display.

TR-8.2: PATCH api/service/{id}. Partial update. If resource_ids is included, it replaces the full set of service_resources in a transaction.

TR-8.3: PATCH api/service/{id}/status. Deactivate service.

TR-8.4: GET api/service. List services scoped to tenant. Returns only active services by default. Accepts optional include_inactive=true for admin users.

---

### TECHNICAL REQUIREMENT 9: Slot Availability Calculation

TR-9.1: Available slots endpoint. GET api/public/services/{serviceId}/slots?resourceId={guid}&date={date}&slug={slug}. The slug parameter resolves the tenant without a JWT. No authentication required. The handler resolves the tenant from the slug, then runs the full slot computation using SlotAvailabilityService.

TR-9.2: The slot calculation must run within 200ms for any date up to 90 days in the future.

TR-9.3: The slot availability endpoint must be publicly accessible with no authentication required.

TR-9.4: The public resources endpoint GET api/public/{slug}/services/{serviceId}/resources must return only resources of type Person that are assigned to the service and have a defined availability schedule. The response must include enough information for the staff selection step (name, role/description, weekly availability summary, list of services offered).

---

### TECHNICAL REQUIREMENT 10: Booking Flow

TR-10.1: POST api/booking. Create booking. Supports two submission paths:

Path A — Guest booking (primary path): no authentication required. Request body must include GuestName, GuestEmail, GuestPhone, ServiceId, ResourceId (or null for auto-assign), ScheduledDate (ISO 8601 date string), ScheduledStartTime (HH:mm string). The handler generates the cancellation token and stores it hashed. The booking is created with UserId null.

Path B — Authenticated booking: requires a valid JWT with role customer. Request body includes the same scheduling fields. GuestName, GuestEmail, GuestPhone are not required; they are sourced from the authenticated user's profile. UserId is set from the JWT claim.

Both paths run inside a serializable transaction with row-level locking. Returns 409 with error code SLOT_UNAVAILABLE if the slot is taken at commit time.

Booking.Create on the domain entity derives ScheduledEndTime, sets initial status based on confirmation mode and price, and appends the first BookingStatusLog automatically. Handlers must not replicate this logic.

TR-10.2: Booking reference format: BK-{YEAR}-{5-digit-zero-padded-sequential-number-per-tenant}. Generated inside CreateBookingHandler by counting existing bookings for the tenant in the current year and adding one, inside the same transaction scope.

TR-10.3: GET api/booking/{id}. Returns booking detail. Accessible by the booking's owner (customer), assigned staff, manager, and admin. Response includes service name, resource name, and customer contact info.

TR-10.4: GET api/booking. Lists bookings scoped to tenant. Supports filtering by status, date range, resource, and service. Supports pagination. Staff see only their assigned resource's bookings. Admins and managers see all.

TR-10.5: PATCH api/booking/{id}/cancel. Cancel booking. Accessible by authenticated customer (own booking only), staff (assigned bookings), manager, and admin. Sets status to Cancelled. Creates refund record if applicable.

TR-10.6: POST api/public/booking/cancel. Guest cancellation endpoint. No authentication required. Accepts raw cancellation token in the request body. System hashes the token, looks up the booking, validates expiry and state, sets status to Cancelled, invalidates the token. Returns 400 if token is expired or already used. Returns 409 if booking is not in a cancellable state.

TR-10.7: GET api/public/booking/lookup?ref={bookingRef}&email={email}. Guest booking lookup. No authentication required. Returns booking details if the reference and email match a booking under any tenant. Does not expose internal IDs. Accessible from the /book/{slug}/lookup page.

TR-10.8: POST api/booking/{id}/staff. Staff-created booking endpoint. Accessible by staff, manager, and admin. Accepts the same fields as TR-10.1 Path A plus an optional existing_customer_email field. If existing_customer_email resolves to a registered customer under the tenant, UserId is set. Otherwise, booking is created as a guest booking.

---

### TECHNICAL REQUIREMENT 11: Payment Collection

TR-11.1: Tenants configure payment gateway credentials via POST api/settings/payment-gateway. Credentials are encrypted at rest. Only one gateway can be active per tenant at a time.

TR-11.2: Payment initiation. When a booking is submitted for a paid service, the client calls POST api/payment/initiate with the booking draft details. The system creates a PayPal order and returns an order ID to the client. The client completes payment on the PayPal side. On return, the client calls POST api/payment/capture with the PayPal order ID. The system captures the payment and creates the booking record.

TR-11.3: Webhook handler. POST api/webhooks/payment. Validates the webhook signature from the gateway before processing. Updates PaymentTransaction status based on the event type. No rate limiting on this endpoint, but signature validation is mandatory.

TR-11.4: Refunds. When a paid booking is cancelled and the cancellation policy permits a refund, the system creates a refund record. POST api/payment/refund/{bookingId} initiates the gateway refund call. Accessible by manager and admin only.

TR-11.5: Manual refund fallback. If the gateway refund call fails, the refund record is marked Failed and the admin is alerted. The admin can reattempt or process manually through the gateway dashboard.

Status: Not built.

---

### TECHNICAL REQUIREMENT 12: Admin Calendar View

TR-12.1: GET api/calendar?date_from={date}&date_to={date}&resource_id={id}&service_id={id}. Accessible by tenant_admin, manager, and staff. Maximum range 31 days. Staff see only their assigned resources' bookings. Response includes bookings and resource availability windows for the date range.

TR-12.2: Calendar events must include: booking reference, service name, service color, resource name, customer name, start time, end time, and status.

Status: Not built.

---

### TECHNICAL REQUIREMENT 13: Email Notifications

TR-13.1: All notifications are sent via an email service interface (IEmailService). The implementation can use any transactional email provider (SendGrid, Mailgun, etc.). The interface is injected; the provider is swappable.

TR-13.2: Notification dispatch is asynchronous. Failures do not block the booking transaction. Failed sends are logged to notification_logs with status failed and retried up to 3 times with exponential backoff.

TR-13.3: Template data per notification type must include: tenant business name, tenant logo URL, tenant theme color, booking reference, service name, specialist name, date, time, duration, and total. Guest notifications must additionally include the raw cancellation URL.

TR-13.4: Booking reminder notifications require a background job scheduler (e.g., Hangfire or a hosted service) that queries upcoming bookings and dispatches reminders at the configured lead time.

Status: Not built.

---

### TECHNICAL REQUIREMENT 14: Audit Logging

TR-14.1: Audit log entries are written after every state-changing operation on: tenants, users, bookings, payments, and refunds.

TR-14.2: Full list of audit events: booking.created, booking.cancelled, booking.rescheduled, booking.status.changed, booking.confirmed, booking.rejected, user.created, user.invited, user.role.changed, user.deactivated, tenant.registered, tenant.status.changed, tenant.settings.updated, payment.captured, refund.initiated, refund.processed, refund.failed.

TR-14.3: Audit log records are insert-only. No update or delete operations are permitted on this table.

Status: Not built.

---

### TECHNICAL REQUIREMENT 15: API Design Standards

TR-15.1: All date fields use ISO 8601 format: YYYY-MM-DD.

TR-15.2: All time fields use 24-hour HH:mm format. Domain entities hold TimeOnly. DTOs and command records hold strings. Conversion from string to TimeOnly happens inside the handler using TimeOnly.ParseExact(value, "HH:mm"). No TimeOnly fields are permitted in command records or DTOs.

TR-15.3: All monetary amounts in API responses are returned as strings to avoid floating-point precision issues.

TR-15.4: All list endpoints must support pagination via pageNumber and pageSize query parameters. Paginated responses use PagedResult<T> directly (not wrapped in BaseResponse). PagedResult includes: data array, total count, page number, page size, and total pages.

TR-15.5: All write endpoints must be idempotent where possible.

TR-15.6: Rate limiting must be applied to: POST api/auth/register (5 per IP per hour), POST api/auth/login (10 per IP per 15 minutes), GET slots endpoint (60 per IP per minute), POST api/webhooks/payment (no rate limit but signature validation required), POST api/public/booking/cancel (20 per IP per hour).

---

### TECHNICAL REQUIREMENT 16: Analytics and Reporting

TR-16.1: GET api/analytics/dashboard?from={date}&to={date}. Accessible by tenant_admin and manager. Returns: total revenue (sum of captured payments), total bookings count, bookings grouped by status, revenue grouped by service, daily booking counts for trend chart, and today's upcoming bookings list.

TR-16.2: All analytics queries must be scoped to the authenticated user's tenant_id. Cross-tenant data access is not permitted.

TR-16.3: Analytics queries must use indexed columns. The bookings (tenant_id, scheduled_date, status) index covers the primary analytics query patterns.

Status: Not built.

---

### TECHNICAL REQUIREMENT 17: Client Directory

TR-17.1: GET api/clients?page={n}&pageSize={n}&search={term}. Accessible by tenant_admin and manager. Returns a paginated list of unique clients who have placed at least one booking under this tenant. Each record includes: name, email, phone, total booking count, total amount spent, and last booking date.

TR-17.2: Client records are derived from the bookings table. A client is uniquely identified by email address within a tenant. If a booking is linked to a UserId, the display name and contact info come from the user profile. If the booking is a guest booking, name, email, and phone come from the guest fields on the booking record.

TR-17.3: GET api/clients/{email}/bookings. Returns the full booking history for a client email under the authenticated tenant. Accessible by tenant_admin and manager.

Status: Not built.

---

## PUBLIC ENDPOINTS PATTERN

Public endpoints live in PublicController under [Route("api/[controller]")] resolving to api/public. All actions use [AllowAnonymous]. Handlers for public endpoints do not call IUserContextService. They resolve tenant by slug using ITenantRepository.FindBySlugAsync(slug). Repository methods called from public handlers use IgnoreQueryFilters() and apply TenantId explicitly via a Where clause because the global ITenantEntity query filter is inactive without a JWT.

Current public endpoints:
GET api/public/{slug} — tenant info and branding.
GET api/public/{slug}/services — active service catalog.
GET api/public/{slug}/services/{serviceId}/resources — staff/resources for a service (used for staff selection step).
GET api/public/services/{serviceId}/slots?resourceId={guid}&date={date}&slug={slug} — available time slots.
POST api/booking — guest booking creation (no auth required, Path A per TR-10.1).
POST api/public/booking/cancel — guest cancellation via token (TR-10.6).
GET api/public/booking/lookup — guest booking lookup by reference and email (TR-10.7).

---

## ACCEPTANCE CRITERIA

The system is complete when the following end-to-end flows work without manual intervention.

A business owner registers, verifies email, configures their profile (including logo and theme color), creates a resource, sets its weekly availability, creates a service with the resource assigned, and configures a payment gateway.

A customer visits /book/{tenant-slug}, sees the business branding, selects a service, selects a date and time, selects a staff member, enters their name, email, and phone number, pays if the service has a price, and sees the success screen with their booking reference. A confirmation email arrives at the provided address containing the booking details and a cancellation link.

The customer clicks the cancellation link in the confirmation email, sees the cancellation confirmation page with the policy and refund eligibility, confirms, and receives a cancellation confirmation email. The slot is freed immediately.

A staff member logs in, sees only their own bookings in the calendar and booking list, and cannot access settings, analytics, or other staff members' bookings.

A Tenant Admin logs in, views the analytics dashboard (revenue metrics, booking trend, today's schedule), sees all bookings in the calendar filtered by service color, views the client directory, and cancels a booking on behalf of a customer. The customer receives a cancellation notification.

A Super Admin logs in, views all tenants on the platform, and suspends one. All users under that tenant are immediately blocked from logging in. The public booking page for that tenant displays a suspension notice.

Every state change in the above flows has a corresponding audit log entry in the database.
