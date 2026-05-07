# MVP Technical Requirements: Multitenant Booking System

## MVP SCOPE PHILOSOPHY

The MVP proves one thing: a business can sign up, configure their services, and accept bookings from customers. Everything else is a scale concern. Cut anything that does not serve that core loop.

---

## DEPENDENCY MAP

Before defining scope, every feature's dependencies must be clear. A feature cannot be built before its dependency is built.

### Dependency Chain (bottom to top)

Level 0 (no dependencies, build first): Tenant Registration, Subscription Plans (platform table, seeded data), Super Admin account (seeded).

Level 1 (depends on Level 0): Tenant Profile Configuration, Tenant Settings Configuration, User and Role Management.

Level 2 (depends on Level 1): Resource Management, Location Management.

Level 3 (depends on Level 2): Service Management, Resource Availability Schedules.

Level 4 (depends on Level 3): Booking Flow (customer-facing), Admin Calendar View.

Level 5 (depends on Level 4): Payment Collection, Notifications, Audit Logging.

Level 6 (depends on Level 5): Refunds, Rescheduling, Cancellation.

### Dependent Features

Booking Flow depends on Services, Resources, Availability Schedules, User Accounts. Payment Collection depends on Booking Flow and Payment Gateway Configuration. Refunds depend on Payment Collection and Cancellation. Cancellation depends on Booking Flow. Rescheduling depends on Booking Flow and Availability Schedules. Notifications depend on Booking Flow. Audit Logging depends on Users, Bookings, Tenants. Tenant Subscription Billing depends on Subscription Plans and Tenant Registration. Role Enforcement depends on User Accounts and Role Definitions.

---

## FEATURES INCLUDED IN MVP

### MVP FEATURE 1: Tenant Registration and Onboarding

Included from use cases: UC-1.1.1, UC-1.1.2, UC-1.1.3 (status management, Super Admin only), UC-1.1.4.

Reason for inclusion: Without this, no tenant exists. This is the entry point of the entire system.

Dependencies: None. This is Level 0.

Dependent features: Everything else in the system.

Status: Built.

---

### MVP FEATURE 2: User and Role Management

Included from use cases: UC-2.1.1, UC-2.1.2, UC-2.2.1, UC-2.2.2.

Reason for inclusion: Without users and roles, there is no authentication boundary, no staff assignment, and no customer identity for bookings.

Dependencies: Tenant Registration.

Dependent features: Booking Flow, Resource Assignment, Calendar View.

Status: Built.

---

### MVP FEATURE 3: Resource Management

Included from use cases: UC-3.1.1, UC-3.1.3, UC-3.1.4.

Reason for inclusion: Resources are what get booked. Without them, no booking slot can be generated.

Dependencies: Tenant Registration, User Accounts.

Dependent features: Service Management, Booking Flow, Calendar View.

Status: Built. The availability schedule save bug (duplicate key on re-save) was fixed by switching SetResourceAvailabilityHandler to use FindByIdWithAvailabilityAsync so EF Core tracks existing child rows correctly.

---

### MVP FEATURE 4: Service Management

Included from use cases: UC-3.1.2.

Reason for inclusion: Services define what customers are booking, how long it takes, and how much it costs.

Dependencies: Resource Management.

Dependent features: Booking Flow.

Status: Built (backend and frontend).

---

### MVP FEATURE 5: Availability Scheduling

Included from use cases: UC-3.1.3, UC-3.1.4.

Reason for inclusion: Without availability rules, the system cannot generate open booking slots.

Dependencies: Resource Management.

Dependent features: Booking Flow.

Status: Built as part of Resource feature (TR-7.4, TR-7.5, TR-7.6). A GET endpoint for reading the saved schedule was added: GET api/resource/{resourceId}/availability. This returns ResourceAvailabilityDto with the full schedule including breaks. The frontend ResourceAvailabilityPage calls this on mount to populate the form with the saved state.

---

### MVP FEATURE 6: Booking Flow

Included from use cases: UC-3.2.1, UC-3.2.2, UC-3.2.4.

Reason for inclusion: This is the core product. A booking system that cannot take bookings is not a booking system.

Note: Rescheduling (UC-3.2.3) is excluded from MVP. Cancellation (UC-3.2.4) is included because it is a minimum viable trust feature. Customers need a way out.

Dependencies: Services, Resources, Availability Schedules, User Accounts.

Dependent features: Payment Collection, Notifications, Audit Logging.

Status: Backend fully built. Tenant Admin frontend fully built. Customer portal built (see below). Login step inside the booking wizard is a documented gap not yet implemented.

#### Customer Portal

The system provides a public booking URL per tenant at /book/{tenant-slug}. Customers learn the slug from the business they are booking with, such as a link on the business website or a printed card. The platform does not expose a public directory of all tenants. This is intentional. The platform is a white-label SaaS. Each tenant owns their customer relationship.

The customer portal consists of two frontend surfaces.

The first is a public landing page at /book/:tenant. This page fetches tenant profile and branding from GET api/public/{slug} and fetches the active service catalog from GET api/public/{slug}/services. It renders the business name, contact details, and list of available services. No authentication is required to view this page.

The second is a multi-step booking wizard at /book/:tenant/new. The wizard handles resource selection, date selection, slot selection, and booking submission. Resources for a selected service are fetched from GET api/public/{slug}/services/{serviceId}/resources. Available slots are fetched from GET api/public/services/{serviceId}/slots?resourceId={guid}&date={date}&slug={slug}. No authentication is required to browse services and slots per TR-9.3.

Authentication is required only at the point of submitting a booking per TR-10.1 Step 1. The customer must be logged in with role customer under the same tenant before the POST api/booking request is accepted. The login prompt step inside the wizard is a documented gap. It is the next item to implement before the end-to-end booking flow can be tested.

All four public endpoints live in PublicController under [Route("api/[controller]")] resolving to api/public. All actions are decorated with [AllowAnonymous]. Handlers for these endpoints do not call IUserContextService. They resolve tenant by slug using ITenantRepository.FindBySlugAsync(slug). Repository methods called from public handlers use IgnoreQueryFilters() and apply TenantId explicitly via a Where clause because the global ITenantEntity query filter is inactive without a JWT.

---

### MVP FEATURE 7: Admin Calendar View

Included from use cases: UC-3.3.1 (week view only).

Reason for inclusion: Staff and admins need to see their schedule. Without this, the system is a black box after bookings are placed.

Dependencies: Booking Flow.

Dependent features: None in MVP.

Status: Not built.

---

### MVP FEATURE 8: Payment Collection

Included from use cases: UC-4.1.1, UC-4.3.1.

Reason for inclusion: If services have a price, the system must collect payment. Without this, paid services cannot be booked end-to-end.

Note: Refunds (UC-4.1.2) are partially included. The system must record refund eligibility and mark a transaction as refunded. The actual gateway refund call is a scale task. In MVP, refunds are processed manually by Tenant Admin by contacting the payment gateway directly, and the admin marks the refund status in the system.

Dependencies: Booking Flow, Payment Gateway Configuration.

Dependent features: Refunds (scale).

Status: Not built.

---

### MVP FEATURE 9: Email Notifications

Included from use cases: UC-5.1.1, subset only.

Included notification types in MVP: Booking Confirmed (to customer), Booking Pending (to customer if manual confirmation mode), Booking Cancelled (to customer), New Booking Alert (to staff or admin).

Excluded from MVP: Booking Reminder (scheduled job complexity), Refund Confirmed (scale), Subscription notifications (scale).

Reason for inclusion: Customers need confirmation they booked. Staff need to know a booking arrived. These two are the minimum for trust.

Dependencies: Booking Flow.

Dependent features: None in MVP.

Status: Not built.

---

### MVP FEATURE 10: Audit Logging

Included from use cases: UC-5.2.1, subset only.

Included events in MVP: booking.created, booking.cancelled, user.created, user.role.changed, tenant.status.changed, payment.captured.

Reason for inclusion: Without any audit trail, debugging production issues is blind. This is a low-cost feature to wire in from day one, and retrofitting it later is expensive.

Dependencies: Users, Bookings, Tenants.

Dependent features: None in MVP.

Status: Not built.

---

## FEATURES EXCLUDED FROM MVP (SCALE BACKLOG)

### SCALE FEATURE S-1: Tenant Subscription and Platform Billing

Excluded use cases: UC-4.2.1, UC-4.2.2, UC-4.2.3.

Reason excluded: Billing complexity is a distraction in MVP. All tenants get full access during the MVP phase. A manual billing process covers the MVP period.

### SCALE FEATURE S-2: Booking Rescheduling

Excluded use cases: UC-3.2.3.

Reason excluded: Cancellation plus re-booking achieves the same outcome. Rescheduling is a convenience feature, not a core one.

### SCALE FEATURE S-3: Automated Refund Processing

Excluded use cases: UC-4.1.2 (gateway refund call only).

Reason excluded: In MVP, refunds are manually handled by Tenant Admin via the payment gateway dashboard. The system records the refund status but does not call the gateway.

### SCALE FEATURE S-4: Booking Reminder Notifications

Excluded use cases: UC-5.1.1, reminder type only.

Reason excluded: Requires a background job scheduler, which adds infrastructure complexity for MVP.

### SCALE FEATURE S-5: Multi-Location Support

Excluded from MVP entirely.

Reason excluded: Most early-stage businesses operate from one location. The locations table is in the ERD but not exposed in the UI.

### SCALE FEATURE S-6: Full Audit Log UI for Tenant Admin

Partially excluded from MVP.

Reason excluded: In MVP, audit logs are written to the database but the Tenant Admin has no UI to read them.

### SCALE FEATURE S-7: Manual Confirmation Mode

Partially excluded from MVP.

Reason excluded: In MVP, all bookings are automatically confirmed. The confirmation_mode column exists in the bookings table but the approval workflow is not activated.

### SCALE FEATURE S-8: Guest Booking

Excluded from MVP.

Reason excluded: Guest booking requires handling anonymous users and booking lookup without login. The guest_name, guest_email, and guest_phone columns exist in the ERD but are not used in MVP.

---

## DETAILED TECHNICAL REQUIREMENTS: MVP

### TECHNICAL REQUIREMENT 1: Platform and Architecture

TR-1.1: The system must be built as a multi-tenant SaaS application where all tenants share a single database instance with row-level tenant isolation enforced by tenant_id on every query.

TR-1.2: The backend must expose a RESTful JSON API. All endpoints return consistent response envelopes. Success: { data: {}, meta: {} }. Error: { error: { code: string, message: string, details: [] } }.

TR-1.3: Every API request to a protected endpoint must pass through two middleware layers in order: authentication middleware (validates session token), then tenant scope middleware (injects tenant_id into the request context and ensures all subsequent queries are filtered by it).

TR-1.4: The frontend must be a single-page application that reads the tenant slug from the URL path and routes accordingly. Two distinct UI surfaces must exist: the Tenant Admin dashboard and the Customer Booking Page.

TR-1.5: The system must support HTTPS only.

TR-1.6: All environment-specific secrets must be stored in environment variables and never committed to source control.

---

### TECHNICAL REQUIREMENT 2: Database

TR-2.1: The following tables must be created for MVP: tenants, tenant_profiles, tenant_settings, tenant_payment_gateways, users, user_profiles, user_resource_assignments, password_reset_tokens, resources, resource_availability_schedules, resource_break_periods, resource_availability_exceptions, services, service_resources, bookings, booking_status_logs, payment_transactions, notification_logs, audit_logs, super_admins, subscription_plans (seeded), locations (table created, not exposed in UI).

TR-2.2: The following tables are defined in the ERD but must NOT be created in MVP: tenant_subscriptions, tenant_invoices, refunds.

TR-2.3: All primary keys must be UUIDs generated by the application layer, not the database.

TR-2.4: Every table must have created_at and updated_at timestamps.

TR-2.5: The following indexes must be created at MVP. bookings: (tenant_id, resource_id, scheduled_date, status). bookings: (tenant_id, user_id). bookings: (tenant_id, scheduled_date). users: (tenant_id, email). audit_logs: (tenant_id, created_at). resource_availability_schedules: (resource_id, day_of_week).

TR-2.6: Database migrations must be versioned and run in order.

---

### TECHNICAL REQUIREMENT 3: Authentication and Session Management

TR-3.1: Authentication must use short-lived JWT access tokens (15-minute expiry) and long-lived refresh tokens (7-day expiry) stored in HTTP-only cookies.

TR-3.2: Login must accept email and password. The access token must contain: user_id, tenant_id, and role.

TR-3.3: On access token expiry, the client must automatically call the refresh endpoint using the refresh token cookie to obtain a new access token without requiring re-login.

TR-3.4: Password hashing must use bcrypt with a minimum cost factor of 12.

TR-3.5: Password reset flow: user submits email, system generates a cryptographically random token stored hashed in password_reset_tokens with 1-hour expiry, system sends reset link, user submits new password with raw token, system validates, updates password, marks token used.

TR-3.6: Invitation flow for staff users: Tenant Admin creates user via POST /users, system creates user with status invited, sends invitation email with raw token, user sets password via accept invitation endpoint, system validates token, checks 72-hour expiry, sets password, sets status to active.

TR-3.7: Super Admin login must use a separate endpoint and a separate token that does not carry a tenant_id.

---

### TECHNICAL REQUIREMENT 4: Role-Based Access Control

TR-4.1: Every protected API endpoint must declare the minimum role required to access it.

TR-4.2: Role hierarchy (highest to lowest): tenant_admin, manager, staff, customer.

TR-4.3: Staff users accessing booking data must be filtered to only return bookings associated with resources assigned to them via user_resource_assignments.

TR-4.4: Customer users must only retrieve and modify their own booking records. Any attempt to access another customer's booking must return 403 Forbidden.

TR-4.5: No endpoint may return data belonging to a different tenant than the one in the authenticated user's token. This must be enforced in the service layer, not only in the controller layer.

---

### TECHNICAL REQUIREMENT 5: Tenant Management

TR-5.1 through TR-5.5: unchanged from original. See original document for full validation rules on registration, email verification, status management, profile update, and settings update.

---

### TECHNICAL REQUIREMENT 6: User Management

TR-6.1 through TR-6.5: unchanged from original. See original document for full rules on staff creation, customer self-registration, role update, deactivation, and list endpoint.

---

### TECHNICAL REQUIREMENT 7: Resource Management

TR-7.1: POST api/resource. Create resource. Accessible by tenant_admin and manager.

TR-7.2: PATCH api/resource/{id}. Update name, description, capacity. ResourceType is frozen after creation and cannot be changed.

TR-7.3: PATCH api/resource/{id}/status. Deactivate resource. Sets is_active to false.

TR-7.4: PUT api/resource/{id}/availability. Full replacement of weekly schedule. Parses HH:mm time strings in the handler. Deletes and re-inserts all child schedule and break period rows in a single tracked EF Core operation.

TR-7.4a (added): GET api/resource/{id}/availability. Returns the current saved schedule as ResourceAvailabilityDto including all days and break periods. Used by the frontend to pre-populate the availability form on load.

TR-7.5: POST api/resource/{id}/exceptions. Add date exception. Exception date must be in the future. Duplicate date and type combination returns 409.

TR-7.6: DELETE api/resource/{id}/exceptions/{exceptionId}. Remove exception.

---

### TECHNICAL REQUIREMENT 8: Service Management

TR-8.1: POST api/service. Create service with resource_ids array (minimum 1).

TR-8.2: PATCH api/service/{id}. Partial update. If resource_ids is included, it replaces the full set of service_resources in a transaction.

TR-8.3: PATCH api/service/{id}/status. Deactivate service.

TR-8.4: GET api/service. List services scoped to tenant. Returns only active services by default. Accepts optional include_inactive=true for admin users.

---

### TECHNICAL REQUIREMENT 9: Slot Availability Calculation

TR-9.1: Available slots endpoint. The active endpoint is GET api/public/services/{serviceId}/slots?resourceId={guid}&date={date}&slug={slug}. The slug parameter is used to resolve the tenant without a JWT. This endpoint is publicly accessible with no authentication required per TR-9.3. The handler resolves the tenant from the slug, then runs the full 9-step slot computation using SlotAvailabilityService.

Note: This endpoint was originally described as GET /services/:id/slots?resource_id=:resource_id&date=:date. It was moved to PublicController and its signature was updated to include slug and to use resourceId (camelCase) as the query parameter name. The original path is no longer active.

TR-9.2: The slot calculation must run within 200ms for any date up to 90 days in the future.

TR-9.3: The slot availability endpoint must be publicly accessible with no auth required.

---

### TECHNICAL REQUIREMENT 10: Booking Flow

TR-10.1: POST api/booking. Create booking. Requires authenticated user with role customer. Runs inside a serializable transaction with row-level locking. Returns 409 with SLOT_UNAVAILABLE if the slot is taken. Booking.Create on the domain entity derives ScheduledEndTime, sets initial status, and appends the first BookingStatusLog automatically. Handlers must not replicate this logic.

Booking reference format: BK-{YEAR}-{5-digit-zero-padded-sequential-number-per-tenant}. Generated inside CreateBookingHandler by counting existing bookings for the tenant in the current year using GetQuery() and adding one, inside the same transaction scope.

TR-10.2 through TR-10.5: unchanged from original. See original document for booking reference generation, get booking, list bookings, and cancel booking rules.

---

### TECHNICAL REQUIREMENT 11: Payment Collection

TR-11.1 through TR-11.5: unchanged from original. See original document for gateway configuration, payment initiation, webhook handling, retry, and manual refund rules.

Status: Not built.

---

### TECHNICAL REQUIREMENT 12: Admin Calendar View

TR-12.1: GET api/calendar?date_from={date}&date_to={date}&resource_id={id}&service_id={id}. Accessible by tenant_admin, manager, and staff. Maximum range 31 days. Staff see only their assigned resources' bookings.

TR-12.2: The calendar endpoint must also return resource availability windows for the requested date range.

Status: Not built.

---

### TECHNICAL REQUIREMENT 13: Email Notifications

TR-13.1 through TR-13.5: unchanged from original. See original document for notification service interface, provider requirements, template data per notification type, async dispatch, and retry rules.

Status: Not built.

---

### TECHNICAL REQUIREMENT 14: Audit Logging

TR-14.1 through TR-14.3: unchanged from original. See original document for middleware-level wiring, full list of MVP audit events, and immutability requirements.

Status: Not built.

---

### TECHNICAL REQUIREMENT 15: API Design Standards

TR-15.1: All date fields use ISO 8601 format: YYYY-MM-DD.

TR-15.2: All time fields use 24-hour HH:mm format. Domain entities hold TimeOnly. DTOs hold strings. Conversion happens inside the handler using TimeOnly.ParseExact(value, "HH:mm").

TR-15.3: All monetary amounts in API responses are returned as strings to avoid floating-point precision issues.

TR-15.4: All list endpoints must support pagination via page and page_size query parameters. Responses include a meta object with total_count, page, page_size, and total_pages.

TR-15.5: All write endpoints must be idempotent where possible.

TR-15.6: Rate limiting must be applied to: POST /register (5 per IP per hour), POST /login (10 per IP per 15 minutes), GET slots endpoint (60 per IP per minute), POST /webhooks/payment (no rate limit, but signature validation required).

---

## PUBLIC ENDPOINTS PATTERN

Public endpoints live in PublicController under [Route("api/[controller]")] resolving to api/public. All actions use [AllowAnonymous]. Handlers for public endpoints do not call IUserContextService. They resolve tenant by slug using ITenantRepository.FindBySlugAsync(slug). Repository methods called from public handlers use IgnoreQueryFilters() and apply TenantId explicitly via a Where clause because the global ITenantEntity query filter is inactive without a JWT.

Current public endpoints: GET api/public/{slug} (tenant info), GET api/public/{slug}/services (service catalog), GET api/public/{slug}/services/{serviceId}/resources (resources for a service), GET api/public/services/{serviceId}/slots?resourceId={guid}&date={date}&slug={slug} (available slots).

---

## MVP ACCEPTANCE CRITERIA SUMMARY

The MVP is complete when the following end-to-end flows work without manual intervention.

A business owner registers, verifies email, configures their profile, creates a resource, sets its availability, creates a service, and configures a payment gateway.

A customer visits /book/{tenant-slug}, browses the service catalog, selects a service, selects a resource, picks a date, picks a slot, logs in as a customer, confirms the booking, pays if the service has a price, and receives a confirmation email with their booking reference.

A Tenant Admin logs in, sees the booking on the calendar, and cancels it. The customer receives a cancellation email. A refund record is created in the database.

A Super Admin logs in, views all tenants, and suspends one. All users under that tenant are immediately blocked from logging in.

Every state change in flows 1 through 4 has a corresponding audit log entry in the database.