Multitenant Booking System: Complete Use Cases

MODULE 1: TENANT MANAGEMENT
Feature 1.1: Tenant Registration
Sub-features: Tenant onboarding, subdomain/slug assignment, tenant profile setup, tenant status management

UC-1.1.1: Register a New Tenant
Actor: Super Admin or Self-Registering Business Owner
Preconditions:

The registrant has a valid email address.
The chosen tenant slug is not already taken.

Main Flow:

The registrant opens the platform's registration page.
The system displays a registration form requesting: business name, owner full name, email address, phone number, preferred subdomain slug (e.g., "citymed"), and password.
The registrant fills in all fields and submits the form.
The system validates that the slug contains only alphanumeric characters and hyphens, is between 3 and 50 characters, and is unique across all tenants.
The system validates that the email address is not already linked to an existing tenant owner account.
The system creates a new tenant record with status set to "pending".
The system creates a dedicated tenant workspace, which includes an isolated data scope, a default configuration profile, and a default admin user account linked to the registrant's email.
The system sends a verification email to the registrant with a confirmation link valid for 24 hours.
The registrant clicks the confirmation link.
The system sets the tenant status to "active" and redirects the registrant to their tenant dashboard.

Alternate Flow A: Slug Already Taken

At step 4, if the slug is taken, the system rejects the form and displays an inline error with a suggestion of available similar slugs.

Alternate Flow B: Email Already Exists

At step 5, the system rejects the form and prompts the registrant to log in or recover their account instead.

Postconditions:

A new tenant exists in the system with status "active".
The tenant owner can log in and access their dashboard.
The tenant's data is fully isolated from all other tenants.


UC-1.1.2: Configure Tenant Profile
Actor: Tenant Admin
Preconditions:

The tenant is active.
The actor is logged in as Tenant Admin.

Main Flow:

The Tenant Admin navigates to Settings > Business Profile.
The system displays the current profile with editable fields: business name, logo, address, timezone, operating hours, contact email, contact phone, website URL, and currency.
The Tenant Admin updates the desired fields and saves.
The system validates all fields. Timezone must be a valid IANA timezone string. Currency must be a valid ISO 4217 code.
The system saves the changes and reflects them immediately across the tenant's booking pages and confirmation emails.

Postconditions:

The tenant profile is updated.
All customer-facing pages reflect the new profile data.


UC-1.1.3: Manage Tenant Status
Actor: Super Admin
Preconditions:

The actor is logged in as Super Admin.

Main Flow:

The Super Admin navigates to the Tenant Management panel.
The system displays a paginated list of all tenants with columns: business name, slug, owner email, status, registration date, and last activity date.
The Super Admin selects a tenant and opens its detail view.
The Super Admin changes the tenant status using a dropdown: active, suspended, or deactivated.
If the status is changed to "suspended", the system immediately blocks all logins for users under that tenant and displays a suspension notice to anyone who tries to access the tenant's booking pages.
If the status is changed to "deactivated", the system blocks access and flags the tenant's data for archival after 30 days.
The system logs the status change with a timestamp and the Super Admin's user ID.

Alternate Flow: Reactivation

The Super Admin changes a suspended tenant's status back to "active".
The system restores full access immediately.

Postconditions:

The tenant status is updated and enforced in real time.


UC-1.1.4: Configure Tenant-Level Settings
Actor: Tenant Admin
Preconditions:

The tenant is active.

Main Flow:

The Tenant Admin navigates to Settings > System Preferences.
The system displays configuration options grouped into sections:

Booking Settings: advance booking window (minimum and maximum days), booking confirmation mode (automatic or manual), cancellation policy, and late cancellation cutoff hours.
Notification Settings: email notification toggles for booking confirmed, booking cancelled, booking reminder, and new customer registered.
Display Settings: date format, time format (12-hour or 24-hour), and language.


The Tenant Admin adjusts values and saves.
The system validates inputs and saves all settings scoped to the tenant.

Postconditions:

All tenant behavior follows the updated settings.


MODULE 2: USER AND ROLE MANAGEMENT
Feature 2.1: User Accounts
Sub-features: User registration, user profile management, user deactivation, password management

UC-2.1.1: Tenant Admin Creates a Staff User
Actor: Tenant Admin
Preconditions:

The tenant is active.
The actor is logged in as Tenant Admin.

Main Flow:

The Tenant Admin navigates to Users > Add New User.
The system displays a form requesting: full name, email address, role, and assigned resources (e.g., specific services or locations the user manages).
The Tenant Admin fills in the form and submits.
The system validates that the email is not already registered under this tenant.
The system creates the user account with status "invited".
The system sends an invitation email to the new user with a link to set their password. The link expires in 72 hours.
The new user clicks the link, sets a password, and logs in.
The system sets the user status to "active".

Alternate Flow: Invitation Expires

If the user does not accept within 72 hours, the Tenant Admin can resend the invitation from the Users panel.

Postconditions:

The new user account exists under the tenant and is accessible.


UC-2.1.2: Customer Self-Registration
Actor: End Customer (Booking User)
Preconditions:

The tenant's booking page is publicly accessible.

Main Flow:

The customer opens the tenant's booking page and clicks "Create Account".
The system displays a form requesting: full name, email address, phone number, and password.
The customer submits the form.
The system validates the email is not already registered under this tenant.
The system creates a customer account scoped to this tenant.
The system sends a verification email.
The customer verifies their email and is redirected to the booking flow.

Postconditions:

The customer account exists and is tied to the tenant's data scope.
The customer's bookings, history, and preferences are stored under this tenant.


Feature 2.2: Role-Based Access Control
Sub-features: Role definitions, permission assignment, role scoping per tenant

UC-2.2.1: Assign and Enforce Roles
Actor: Tenant Admin
Preconditions:

The user to be assigned a role already exists under the tenant.

Roles and Permissions Definitions:
Tenant Admin:

Full access to all settings, users, bookings, reports, and billing.
Cannot access other tenants' data.

Manager:

Access to all bookings and customer records.
Access to reports.
Cannot change billing or tenant-level settings.
Cannot create or delete staff users.

Staff:

Access only to bookings assigned to their resource or service.
Cannot view other staff's bookings unless granted explicitly.
Cannot access reports or settings.

Customer:

Access only to their own bookings and profile.
No access to any admin area.

Main Flow:

The Tenant Admin navigates to Users and selects a user.
The system displays the user's current role.
The Tenant Admin changes the role via a dropdown.
The system saves the new role and updates the user's permission set immediately.
On the user's next page load or action, the system enforces the new role's permission boundaries.

Postconditions:

The user's access is limited or expanded according to their new role.


UC-2.2.2: Permission Enforcement on Data Access
Actor: System (automated enforcement)
Preconditions:

A logged-in user makes a request to view or modify data.

Main Flow:

The user sends a request to any protected endpoint (e.g., view all bookings, edit a resource, access reports).
The system extracts the user's tenant ID and role from their session token.
The system checks the request against the permission matrix for that role.
If the user's role permits the action, the system returns the requested data scoped to their tenant.
If the user's role does not permit the action, the system returns a 403 Forbidden response and logs the attempt.

Postconditions:

No user accesses data outside their tenant scope or role boundary.


MODULE 3: BOOKING AND SCHEDULING
Feature 3.1: Resource and Service Setup
Sub-features: Resource creation, service definition, availability rules, buffer time, capacity settings

UC-3.1.1: Create a Bookable Resource
Actor: Tenant Admin or Manager
Preconditions:

The tenant is active.

Main Flow:

The actor navigates to Resources > Add Resource.
The system displays a form requesting: resource name, description, resource type (person, room, equipment, or slot-based), capacity (maximum concurrent bookings), and assigned location if multi-location is enabled.
The actor fills in the form and saves.
The system creates the resource and makes it available for service assignment.

Examples of resource types:

Person: a doctor, consultant, instructor, or staff member.
Room: a conference room, studio, or treatment room.
Equipment: a machine, vehicle, or tool.
Slot-based: a general time slot with no specific person or room attached.

Postconditions:

The resource is created and ready to be linked to services and availability schedules.


UC-3.1.2: Define a Service
Actor: Tenant Admin or Manager
Preconditions:

At least one resource exists.

Main Flow:

The actor navigates to Services > Add Service.
The system displays a form requesting: service name, description, duration (in minutes), price, currency (defaulting to tenant currency), buffer time before and after (in minutes), maximum advance booking (days), minimum advance booking (hours), cancellation policy override (optional), and assigned resources.
The actor fills in the form and saves.
The system creates the service and links it to the selected resources.
The service becomes available in the booking flow for customers.

Postconditions:

The service is created and bookable by customers through assigned resources.


UC-3.1.3: Set Resource Availability
Actor: Tenant Admin, Manager, or Staff (for their own resource)
Preconditions:

The resource exists.

Main Flow:

The actor navigates to Resources > select resource > Availability.
The system displays a weekly schedule grid with rows for each day of the week.
The actor sets working hours for each day by selecting start time and end time or marking the day as unavailable.
The actor adds break periods within a day if needed (e.g., 12:00 PM to 1:00 PM).
The actor saves the schedule.
The system stores the recurring availability rule for the resource.
The system uses this schedule as the base availability when generating open booking slots.

Postconditions:

The resource has a defined weekly availability schedule.
Customers can only book within these hours.


UC-3.1.4: Add Availability Exceptions
Actor: Tenant Admin, Manager, or Staff
Preconditions:

The resource has a base availability schedule.

Main Flow:

The actor navigates to Resources > select resource > Exceptions.
The actor clicks "Add Exception".
The system displays a form requesting: date or date range, exception type (unavailable all day, unavailable for specific hours, or available outside normal hours), and optional note.
The actor fills in the form and saves.
The system stores the exception and overrides the base schedule for the specified date(s).
Any existing bookings that fall within the exception window are flagged for review but not automatically cancelled.

Postconditions:

The resource reflects the exception on its availability calendar.
New bookings cannot be placed in the exception window.


Feature 3.2: Booking Flow
Sub-features: Slot selection, customer details collection, booking confirmation, booking reference generation

UC-3.2.1: Customer Creates a Booking
Actor: End Customer
Preconditions:

The tenant's booking page is accessible.
At least one service with an available resource and open time slots exists.

Main Flow:

The customer opens the tenant's booking page.
The system displays available services.
The customer selects a service.
The system displays available resources linked to that service (if multiple exist, the customer selects one, or the system auto-assigns based on availability).
The system displays a calendar showing available dates based on the resource's schedule, exceptions, existing bookings, buffer times, and the service's advance booking rules.
The customer selects a date.
The system displays available time slots for that date, calculated by: taking the resource's working hours, subtracting existing bookings and their buffer times, and dividing the remaining time into slots matching the service duration.
The customer selects a time slot.
If the customer is not logged in, the system prompts them to log in or continue as a guest (if guest booking is enabled by the tenant).
The system displays a booking summary: service name, resource, date, time, duration, and price.
The customer confirms the details and proceeds to payment (if the service has a price) or directly submits the booking (if free).
On submission, the system checks that the selected slot is still available (concurrency check).
If available, the system creates the booking record with status "confirmed" (automatic confirmation) or "pending" (manual confirmation), depending on tenant settings.
The system generates a unique booking reference number (e.g., BK-2026-00842).
The system sends a confirmation email to the customer with booking details and the reference number.
The system sends a notification email or in-app alert to the assigned staff or resource manager.

Alternate Flow A: Slot No Longer Available (Concurrency Conflict)

At step 12, if another booking was placed in the same slot between steps 8 and 12, the system notifies the customer and returns them to step 7 to select a new slot.

Alternate Flow B: Manual Confirmation Mode

The booking is created with status "pending".
The Tenant Admin or Manager reviews the booking and either confirms or rejects it.
The system notifies the customer of the outcome.

Postconditions:

A booking record exists with a unique reference.
The slot is reserved and no longer available to other customers.
Both customer and staff receive notifications.


UC-3.2.2: View Booking Details
Actor: Customer, Staff, Manager, or Tenant Admin
Preconditions:

A booking exists.

Main Flow:

The actor navigates to their bookings list (customers see their own, staff see assigned bookings, managers and admins see all bookings).
The system displays a list of bookings with columns: reference number, service, resource, date, time, customer name, status, and payment status.
The actor clicks a booking to open its detail view.
The system displays full booking details: all fields from the list plus booking notes, customer contact info, cancellation policy, and payment history.

Postconditions:

The actor views the booking details within their permission scope.


UC-3.2.3: Reschedule a Booking
Actor: Customer or Staff
Preconditions:

The booking status is "confirmed" or "pending".
The reschedule is requested before the cancellation cutoff defined by the tenant.

Main Flow:

The actor opens the booking detail view.
The actor clicks "Reschedule".
The system displays the availability calendar for the same resource and service.
The actor selects a new date and time slot.
The system displays a summary showing the original and new date/time.
The actor confirms the reschedule.
The system updates the booking record with the new date and time, releases the original slot, and reserves the new slot.
The system sends a reschedule confirmation email to the customer.

Alternate Flow: No Available Slots

If no slots are available within the advance booking window, the system informs the actor and suggests contacting the business directly.

Postconditions:

The booking is updated to the new date and time.
The original slot is freed.
Both parties receive updated confirmation.


UC-3.2.4: Cancel a Booking
Actor: Customer, Staff, Manager, or Tenant Admin
Preconditions:

The booking is not already cancelled or completed.

Main Flow:

The actor opens the booking detail view.
The actor clicks "Cancel Booking".
The system checks whether the cancellation is within the tenant's allowed cancellation window.
The system displays a cancellation summary showing whether a refund applies based on the cancellation policy.
The actor confirms the cancellation and optionally enters a reason.
The system sets the booking status to "cancelled".
The system releases the slot back to the resource's availability.
If a refund applies, the system initiates the refund process (see Payments module).
The system sends a cancellation confirmation email to the customer.

Alternate Flow: Late Cancellation

If the cancellation is past the allowed window, the system notifies the actor that the cancellation policy applies and no refund will be issued (or a partial refund, per policy).
The actor proceeds or abandons the cancellation.

Postconditions:

The booking status is "cancelled".
The slot is freed.
Refund is processed if applicable.


Feature 3.3: Calendar and Schedule Management
Sub-features: Admin calendar view, resource-level calendar, multi-resource view

UC-3.3.1: View the Booking Calendar
Actor: Tenant Admin, Manager, or Staff
Preconditions:

The actor is logged in.

Main Flow:

The actor navigates to Calendar.
The system displays a calendar defaulting to the current week in a day-column grid view.
Each booking appears as a block on the calendar at its scheduled time, color-coded by resource or service (configurable).
The actor switches between day view, week view, and month view using tabs.
The actor filters the calendar by resource, service, or staff using filter controls.
The actor clicks a booking block to open a summary popup with options to view full details, reschedule, or cancel.
Staff users see only their assigned resource's bookings. Managers and Admins see all bookings.

Postconditions:

The actor has a real-time view of the booking schedule.


MODULE 4: PAYMENTS AND BILLING
Feature 4.1: Payment Collection
Sub-features: Online payment at booking, partial payment, payment status tracking

UC-4.1.1: Customer Pays at Booking
Actor: End Customer
Preconditions:

The selected service has a price greater than zero.
The tenant has a payment gateway configured.

Main Flow:

After the customer confirms booking details (UC-3.2.1, step 10), the system displays a payment step.
The system shows the total amount due, service name, and accepted payment methods (e.g., credit card, debit card).
The customer enters payment details or selects a saved payment method if they have one on file.
The customer submits payment.
The system sends the payment request to the configured payment gateway.
The gateway processes the payment and returns a success or failure response.
On success: the system records the transaction with status "paid", links it to the booking, and proceeds to create the confirmed booking record (UC-3.2.1, step 13 onward).
On failure: the system displays an error message specifying the reason (card declined, insufficient funds, or gateway error) and prompts the customer to retry or use a different payment method.

Alternate Flow: Free Service

If the service price is zero, the payment step is skipped entirely.

Postconditions:

A payment transaction record exists linked to the booking.
The booking is confirmed only after successful payment.


UC-4.1.2: Process a Refund
Actor: Tenant Admin or Manager
Preconditions:

A paid booking has been cancelled and the cancellation policy permits a refund.

Main Flow:

The system triggers refund eligibility check automatically when a booking is cancelled (from UC-3.2.4).
If the cancellation policy permits a full or partial refund, the system creates a refund record with status "pending".
The Tenant Admin or Manager reviews the refund queue under Payments > Refunds.
The actor selects the pending refund and clicks "Process Refund".
The system sends a refund request to the payment gateway for the eligible amount.
The gateway processes the refund and returns confirmation.
The system updates the refund record status to "completed" and the original transaction record to reflect the partial or full refund.
The system sends a refund confirmation email to the customer with the refund amount and expected processing time.

Alternate Flow: Refund Rejected by Gateway

The system updates the refund status to "failed" and alerts the Tenant Admin.
The Tenant Admin contacts the customer manually to resolve.

Postconditions:

The refund is processed and recorded.
The customer receives confirmation.


Feature 4.2: Tenant Billing (Platform Subscription)
Sub-features: Subscription plan management, invoice generation, payment failure handling

UC-4.2.1: Tenant Subscribes to a Plan
Actor: Tenant Admin
Preconditions:

The tenant is active.
The tenant has not yet subscribed or is changing their plan.

Main Flow:

The Tenant Admin navigates to Billing > Plans.
The system displays available subscription plans with pricing, billing cycle (monthly or annual), and feature limits (e.g., maximum resources, maximum bookings per month, number of staff users).
The Tenant Admin selects a plan and clicks "Subscribe".
The system displays a checkout form requesting billing email and credit card details.
The Tenant Admin submits the form.
The system sends the subscription request to the platform's billing provider.
The billing provider creates a recurring subscription and charges the first billing cycle.
On success, the system sets the tenant's subscription status to "active" and records the plan details.
The system sends an invoice to the Tenant Admin's billing email.

Postconditions:

The tenant has an active subscription.
Platform features are unlocked according to the plan's limits.


UC-4.2.2: Handle Failed Subscription Payment
Actor: System (automated), Tenant Admin
Preconditions:

The tenant has an active subscription.
A scheduled payment attempt fails.

Main Flow:

The billing provider notifies the system of a failed payment via webhook.
The system sets the tenant's subscription status to "past due".
The system sends an email to the Tenant Admin with the failure reason and a link to update payment details.
The system retries the payment after 3 days and again after 7 days.
If payment succeeds on retry, the subscription status returns to "active" and the Tenant Admin is notified.
If all retries fail after 14 days, the system sets the subscription status to "suspended" and restricts access to paid features.
The tenant's existing data is preserved but new bookings are blocked from being created.

Postconditions:

The subscription status reflects the payment state.
The tenant is notified at each stage.


UC-4.2.3: Tenant Admin Views Invoices
Actor: Tenant Admin
Preconditions:

The tenant has at least one billing cycle recorded.

Main Flow:

The Tenant Admin navigates to Billing > Invoices.
The system displays a list of invoices with columns: invoice number, billing period, amount, status (paid, pending, failed), and date.
The Tenant Admin clicks an invoice to view its details.
The system displays the full invoice with line items, subtotal, tax, and total.
The Tenant Admin downloads the invoice as a PDF.

Postconditions:

The Tenant Admin has access to their full billing history.


Feature 4.3: Payment Gateway Configuration
Sub-features: Gateway setup, API credential storage, test mode and live mode

UC-4.3.1: Configure a Payment Gateway
Actor: Tenant Admin
Preconditions:

The tenant has an account with a supported payment gateway (e.g., Stripe, PayPal, or Paymongo).

Main Flow:

The Tenant Admin navigates to Settings > Payments.
The system displays a list of supported payment gateways.
The Tenant Admin selects a gateway and clicks "Configure".
The system displays a form requesting the gateway's API credentials (publishable key and secret key).
The Tenant Admin enters the credentials and selects mode: Test or Live.
The Tenant Admin saves the configuration.
The system encrypts and stores the credentials scoped to the tenant.
The system runs a validation request to the gateway using the provided credentials to confirm they are valid.
If valid, the system marks the gateway as "connected" and enables payment collection on the tenant's booking pages.
If invalid, the system displays an error and prompts the Tenant Admin to check their credentials.

Postconditions:

The tenant's booking flow accepts payments through the configured gateway.
Credentials are stored encrypted and never exposed in plaintext to any user.


CROSS-CUTTING CONCERNS
Feature 5.1: Notifications
UC-5.1.1: Send Automated Notifications
Actor: System
Triggers and notification rules:

Booking Confirmed: system sends email to customer with booking reference, service details, date, time, location or virtual link, and cancellation policy.
Booking Pending: system sends email to customer stating the booking is awaiting confirmation.
Booking Confirmed by Admin (Manual Mode): system sends email to customer confirming the booking is now confirmed.
Booking Rejected by Admin: system sends email to customer with rejection reason if provided.
Booking Cancelled by Customer: system sends cancellation confirmation to customer and alert to staff.
Booking Cancelled by Staff or Admin: system sends cancellation notification to customer with reason.
Booking Rescheduled: system sends updated confirmation to customer.
Booking Reminder: system sends reminder email to customer 24 hours before the scheduled time (timing configurable by tenant).
New Booking Alert to Staff: system sends alert to the assigned resource's staff member when a new booking is placed.

All notification content is scoped to the tenant and uses the tenant's business name, logo, and contact details in the email template.

Feature 5.2: Audit Logging
UC-5.2.1: Record System Events
Actor: System (automated)
Main Flow:

Every significant action in the system generates an audit log entry.
Each entry records: timestamp, actor user ID, actor role, tenant ID, action type (e.g., booking.created, user.role.changed, tenant.suspended), affected record ID, and IP address.
Audit logs are immutable. No user, including Super Admin, can delete or edit them.
Super Admin accesses audit logs for any tenant via the Super Admin panel.
Tenant Admin accesses audit logs scoped only to their tenant via Settings > Audit Log.
Logs are retained for a minimum of 90 days and exportable as CSV.

Postconditions:

Every user action and system event is traceable.
Compliance and debugging are supported by a full audit trail.