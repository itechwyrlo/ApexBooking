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

Alternate Flow C: Onboarding Request (Manual Approval)

A business owner submits an onboarding request from the platform's public onboarding page without self-provisioning.
The Super Admin reviews pending requests in the Tenant Management panel.
The Super Admin approves the request: the system provisions the tenant workspace and sends a welcome and setup email to the business owner.
Alternatively, the Super Admin rejects the request and provides a reason. The system sends a rejection email to the applicant stating the reason so they can address it and reapply.
Plan tier (e.g., Professional or Enterprise) is recorded at the time of approval.

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

The Tenant Admin navigates to Settings > Organization.
The system displays the current profile with editable fields: business name, logo, theme color, slug, industry, description, address, timezone, operating hours, contact email, contact phone, website URL, and currency.
The Tenant Admin updates the desired fields and saves.
The system validates all fields. Timezone must be a valid IANA timezone string. Currency must be a valid ISO 4217 code.
The system saves the changes and reflects them immediately across the tenant's public booking pages and confirmation email templates.
The tenant's public booking page renders the uploaded logo and theme color so customers experience the business's branding.

Postconditions:

The tenant profile is updated.
All customer-facing booking pages and emails reflect the business name, logo, theme color, and contact details.


UC-1.1.3: Manage Tenant Status
Actor: Super Admin
Preconditions:

The actor is logged in as Super Admin.

Main Flow:

The Super Admin navigates to the Tenant Management panel.
The system displays a paginated list of all tenants with columns: business name, slug, owner email, status, plan tier, registration date, and last activity date.
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

Booking Settings: advance booking window (minimum hours and maximum days), booking confirmation mode (automatic or manual), cancellation policy, and late cancellation cutoff hours.
Notification Settings: email notification toggles for booking confirmed, booking cancelled, booking reminder, and new customer registered.
Display Settings: date format, time format (12-hour or 24-hour), and language.

The Tenant Admin adjusts values and saves.
The system validates inputs and saves all settings scoped to the tenant.

Postconditions:

All tenant behavior follows the updated settings.


MODULE 2: USER AND ROLE MANAGEMENT
Feature 2.1: User Accounts
Sub-features: Staff user management, optional customer account registration, guest booking lookup

UC-2.1.1: Tenant Admin Creates a Staff User
Actor: Tenant Admin
Preconditions:

The tenant is active.
The actor is logged in as Tenant Admin.

Main Flow:

The Tenant Admin navigates to Staff > Add New Staff Member.
The system displays a form requesting: full name, email address, role, and assigned services (the services this staff member delivers).
The Tenant Admin fills in the form and submits.
The system validates that the email is not already registered under this tenant.
The system creates the user account with status "invited".
The system sends an invitation email to the new user with a link to set their password. The link expires in 72 hours.
The new user clicks the link, sets a password, and logs in.
The system sets the user status to "active".

Alternate Flow: Invitation Expires

If the user does not accept within 72 hours, the Tenant Admin can resend the invitation from the Staff panel.

Postconditions:

The new staff user account exists under the tenant and is accessible.


UC-2.1.2: Customer Account Registration (Optional)
Actor: End Customer (Booking User)
Preconditions:

The tenant's booking page is publicly accessible.
The customer has placed one or more bookings as a guest, or is choosing to register proactively.

Main Flow:

The customer opens the tenant's booking page and clicks "Create Account", or clicks a "Create Account" link included in a post-booking confirmation email.
The system displays a registration form requesting: full name, email address, phone number, and password.
The customer submits the form.
The system validates the email is not already registered under this tenant.
The system creates a customer account scoped to this tenant with status pending verification.
The system sends a verification email.
The customer verifies their email and is redirected to their booking history.
The system automatically links all past bookings placed with that email address under this tenant to the newly created account.

Alternate Flow A: Email Already Registered

At step 4, if the email is already registered under this tenant, the system prompts the customer to log in instead and does not create a duplicate account.

Alternate Flow B: Proactive Registration Before Booking

A customer may register before making their first booking. Once registered and verified, all subsequent bookings made with the same email will be linked to their account automatically.

Postconditions:

The customer account exists and is scoped to this tenant only.
All bookings previously placed with the same email address under this tenant are linked to the account.


UC-2.1.3: Customer Looks Up a Booking (No Account Required)
Actor: End Customer (Guest, unauthenticated)
Preconditions:

The customer has received a booking confirmation email containing a booking reference number.

Main Flow:

The customer navigates to the booking lookup page at /book/{tenant-slug}/lookup.
The customer enters their booking reference number and the email address used when booking.
The system retrieves the matching booking record scoped to this tenant.
The system displays the booking details: service, specialist, date, time, duration, total, and current status.
If the booking is in a cancellable state and within the tenant's cancellation window, the system presents a Cancel option.
The customer requests cancellation. The system sends a secure cancellation confirmation link to the customer's email address to complete the action (see UC-3.2.5).

Postconditions:

The customer can view their specific booking without an account.
No other customer's data is accessible through this lookup.


Feature 2.2: Role-Based Access Control
Sub-features: Role definitions, permission assignment, role scoping per tenant

UC-2.2.1: Assign and Enforce Roles
Actor: Tenant Admin
Preconditions:

The user to be assigned a role already exists under the tenant.

Roles and Permissions Definitions:
Tenant Admin:

Full access to all settings, staff, bookings, analytics dashboard, client directory, and billing.
Cannot access other tenants' data.

Manager:

Access to all bookings and client records.
Access to analytics dashboard.
Cannot change billing or tenant-level settings.
Cannot create or delete staff users.

Staff:

Access only to bookings assigned to their resource or service.
Cannot view other staff members' bookings.
Cannot access analytics, client directory, or settings.
Can manage their own availability schedule and profile.

Customer (authenticated):

Access only to their own bookings through the customer portal.
No access to any admin or staff area.

Main Flow:

The Tenant Admin navigates to Staff and selects a user.
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

The user sends a request to any protected endpoint (e.g., view all bookings, edit a resource, access analytics).
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

The actor navigates to Staff > Add Resource, or to the Resources section.
The system displays a form requesting: resource name, description, resource type (Person, Room, Equipment, or Slot-based), and capacity (maximum concurrent bookings).
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
The system displays a form requesting: service name, description, duration (in minutes), price, currency (defaulting to tenant currency), color code (used on the admin calendar for visual distinction), buffer time before and after (in minutes), maximum advance booking (days), minimum advance booking (hours), cancellation policy override (optional), and assigned resources.
The actor fills in the form and saves.
The system creates the service and links it to the selected resources.
The service becomes visible on the tenant's public booking page at /book/{tenant-slug}.

Postconditions:

The service is created and bookable by customers through the assigned resources.


UC-3.1.3: Set Resource Availability
Actor: Tenant Admin, Manager, or Staff (for their own resource)
Preconditions:

The resource exists.

Main Flow:

The actor navigates to Staff > select staff member > Availability, or Resources > select resource > Availability.
The system displays a weekly schedule grid with rows for each day of the week.
The actor sets working hours for each day by selecting start time and end time, or marks the day as unavailable.
The actor adds break periods within a day if needed (e.g., 12:00 to 13:00).
The actor saves the schedule.
The system stores the recurring weekly availability rule for the resource.
The system uses this schedule as the base when computing available booking slots for customers.

Postconditions:

The resource has a defined weekly availability schedule.
Customers can only book within these hours.


UC-3.1.4: Add Availability Exceptions
Actor: Tenant Admin, Manager, or Staff
Preconditions:

The resource has a base availability schedule.

Main Flow:

The actor navigates to Resources > select resource > Exceptions.
The actor clicks Add Exception.
The system displays a form requesting: date or date range, exception type (unavailable all day, unavailable for specific hours, or available outside normal hours), and an optional note.
The actor fills in the form and saves.
The system stores the exception and overrides the base schedule for the specified date(s).
Any existing bookings that fall within the exception window are flagged for review but are not automatically cancelled.

Postconditions:

The resource reflects the exception on its availability calendar.
New bookings cannot be placed in the exception window.


Feature 3.2: Booking Flow
Sub-features: Service selection, date and time selection, staff selection, contact details collection, booking confirmation, booking reference generation, guest cancellation, staff-created bookings

UC-3.2.1: Customer Creates a Booking
Actor: End Customer (Guest — no account required)
Preconditions:

The tenant's public booking page at /book/{tenant-slug} is accessible.
At least one active service with an assigned resource and a defined availability schedule exists.

Main Flow:

The customer opens the tenant's public booking page.
The system displays the tenant's branding (business name, logo, theme color, address) and the list of active services. Each service entry shows its name, description, duration, and price.
The customer selects a service and clicks Continue.
The system displays a combined date and time selection screen. The calendar shows available dates constrained by: the assigned resources' weekly availability schedules, availability exceptions, the service's minimum advance booking hours, and the service's maximum advance booking days. Dates with no open slots are disabled.
The customer selects a date. The system immediately computes and displays available time slots for that date. Slots are derived from: the resource's working hours for that day, minus existing confirmed or pending bookings and their configured buffer times before and after, split into intervals matching the service duration. Break periods are excluded. The booking selection summary bar updates to show the selected service and price.
The customer selects a time slot and clicks Continue.
The system displays the staff selection step. It shows only the staff members (resources of type Person) assigned to this service who are available for the selected date and time slot. An "Any Available" option is always presented first and will auto-assign the best available staff member at booking time. The customer may click an info icon on any staff card to view that staff member's weekly availability and the services they offer.
The customer selects a staff member (or Any Available) and clicks Continue. The selection summary bar updates to show the selected date, time, and specialist.
The system displays a contact details form requesting: full name (required), email address (required), phone number (required), and notes (optional). No login or account creation is prompted at any step.
The customer fills in their contact details and clicks Confirm Booking.
If the service has a price greater than zero and the tenant has a payment gateway configured, the system presents a payment step (see UC-4.1.1) before creating the booking record.
The system performs a final slot availability check inside a serializable transaction with row-level locking to prevent double-booking.
If the slot is still available, the system creates the booking record with: the contact details provided, the selected or auto-assigned resource, ScheduledEndTime derived from the service duration added to the selected start time, and BookingStatus set to Confirmed (automatic mode) or Pending (manual confirmation mode).
The system generates a unique booking reference in the format BK-{YEAR}-{5-digit-zero-padded-sequential-number} scoped per tenant per year.
The system generates a cryptographically random cancellation token, stores it hashed in the booking record, and sets an expiry window aligned to the tenant's cancellation policy cutoff.
The system sends a booking confirmation email to the provided email address containing: booking reference, business name, service name, specialist name, date, time, duration, total amount, cancellation policy, and a secure cancellation link containing the raw token.
The system sends a new booking alert to the assigned staff member and the tenant admin.
The system displays a success screen: "You're all set! A confirmation email has been sent to {email}" with the full booking summary (name, service, specialist, date, time, duration, total) and a "Book Another" button.

Alternate Flow A: Slot No Longer Available (Concurrency Conflict)

At step 12, if another booking was placed in the same slot between steps 6 and 12, the system notifies the customer that the slot is no longer available and returns them to step 5 to select a new slot. Any payment already captured is held pending slot re-selection.

Alternate Flow B: Manual Confirmation Mode

At step 13, the booking is created with status Pending instead of Confirmed.
The system sends a Booking Pending email to the customer.
The Tenant Admin or Manager reviews the pending booking and confirms or rejects it.
The system notifies the customer of the outcome by email.

Alternate Flow C: Authenticated Customer

If the customer is logged in as an authenticated customer under this tenant when they reach step 9, the contact form is pre-populated from their account profile. The booking is linked to their UserId. No cancellation token is generated; they cancel through their customer portal instead.

Postconditions:

A booking record exists with a unique reference and an initial status.
The slot is reserved and no longer available to other customers for the same resource and time.
The customer receives a confirmation email with full booking details and, for guest bookings, a secure cancellation link.
The assigned staff member and admin receive a new booking alert.


UC-3.2.2: View Booking Details
Actor: Authenticated Customer, Staff, Manager, or Tenant Admin
Preconditions:

A booking exists.

Main Flow:

The actor navigates to their bookings view. Authenticated customers see their own bookings in the customer portal. Staff see only bookings assigned to their resource. Managers and Admins see all bookings under the tenant.
The system displays a filterable, searchable list of bookings with columns: reference number, service, specialist, date, time, customer name, status badge, and payment status badge.
The actor selects a booking to open its detail view.
The system displays full booking details: all list fields plus customer contact info (name, email, phone), booking notes, status history log, cancellation policy, and payment history.

Postconditions:

The actor views the booking details within their permission scope.


UC-3.2.3: Reschedule a Booking
Actor: Authenticated Customer or Staff
Preconditions:

The booking status is Confirmed or Pending.
The reschedule is requested before the cancellation cutoff defined by the tenant.

Main Flow:

The actor opens the booking detail view.
The actor clicks Reschedule.
The system displays the availability calendar for the same service and resource.
The actor selects a new date and time slot.
The system displays a summary showing the original and new date and time.
The actor confirms the reschedule.
The system updates the booking record with the new date and time, releases the original slot, and reserves the new slot.
The system sends a reschedule confirmation email to the customer.

Alternate Flow A: No Available Slots

If no slots are available within the advance booking window, the system informs the actor and suggests contacting the business directly.

Alternate Flow B: Guest Reschedule Request

Guest customers cannot reschedule self-service. They contact the business directly via the contact information in their confirmation email. Staff or Admins reschedule the booking on their behalf from the admin bookings view.

Postconditions:

The booking is updated to the new date and time.
The original slot is freed.
Both parties receive updated confirmation.


UC-3.2.4: Cancel a Booking (Authenticated Actor)
Actor: Authenticated Customer, Staff, Manager, or Tenant Admin
Preconditions:

The booking is not already cancelled or completed.
The actor is logged in.

Main Flow:

The actor opens the booking detail view.
The actor clicks Cancel Booking.
The system checks whether the cancellation is within the tenant's allowed cancellation window.
The system displays a cancellation summary showing whether a refund applies based on the cancellation policy.
The actor confirms the cancellation and optionally enters a reason.
The system sets the booking status to Cancelled.
The system releases the slot back to the resource's availability.
If a refund applies, the system creates a refund record with status Pending (see UC-4.1.2).
The system sends a cancellation confirmation email to the customer.

Alternate Flow: Late Cancellation

If the cancellation is past the allowed window, the system notifies the actor that the cancellation policy applies and no refund will be issued (or a partial refund per policy). The actor proceeds or abandons the cancellation.

Postconditions:

The booking status is Cancelled.
The slot is freed.
Refund is initiated if applicable per policy.


UC-3.2.5: Guest Cancels a Booking via Secure Link
Actor: End Customer (Guest, unauthenticated)
Preconditions:

The booking was created as a guest booking (no UserId linked to the record).
The customer has a confirmation email containing a secure cancellation link.
The booking is not already cancelled or completed.

Main Flow:

The customer clicks the cancellation link in their confirmation email. The link contains a raw cancellation token as a URL query parameter at /book/{tenant-slug}/cancel?token={raw-token}.
The system hashes the raw token and looks up the matching booking record.
The system validates that the token has not expired and the booking is in a cancellable state.
The system displays a confirmation page showing: booking reference, service name, specialist, date, time, and the tenant's cancellation policy with applicable refund eligibility.
The customer confirms the cancellation.
The system sets the booking status to Cancelled.
The system marks the cancellation token as used. It cannot be reused.
The system releases the slot back to the resource's availability.
If a refund applies per the cancellation policy, the system creates a refund record with status Pending.
The system sends a cancellation confirmation email to the customer.

Alternate Flow A: Token Expired

The system displays a message stating the cancellation link has expired and shows the business's contact information so the customer can request cancellation directly.

Alternate Flow B: Token Already Used

The system displays a message stating the booking has already been cancelled.

Alternate Flow C: Late Cancellation

The system informs the customer that the cancellation window has closed per the tenant's policy. The customer may still proceed but no refund will be issued.

Postconditions:

The booking status is Cancelled.
The slot is freed.
The cancellation token is permanently invalidated.
Refund is initiated if applicable per policy.


UC-3.2.6: Staff Creates a Booking on Behalf of a Customer
Actor: Tenant Admin, Manager, or Staff
Preconditions:

The actor is logged in with at least Staff role.
A customer is present in person (walk-in) or is contacting the business by phone.

Main Flow:

The actor navigates to Bookings > New Booking in the admin dashboard.
The system displays the active service catalog. The actor selects a service.
The system displays a combined date and time selection screen with the same slot computation as UC-3.2.1. The actor selects a date and time slot.
The system displays available staff. The actor selects a staff member or accepts auto-assignment.
The system displays a customer contact form requesting: full name, email address, and phone number.
The actor may search for an existing customer by email. If a match is found under this tenant, the system pre-populates the form from the existing record.
The actor fills in or confirms the contact details and submits.
The system performs the same slot availability check as UC-3.2.1.
The system creates the booking record. If an existing customer account was resolved in step 6, the booking is linked to their UserId. Otherwise, the booking is created as a guest booking with the provided contact details.
The system generates a unique booking reference.
The system sends a confirmation email to the customer's provided email address.

Postconditions:

The booking exists in the system with a unique reference.
The slot is reserved.
The customer receives a confirmation email.


Feature 3.3: Calendar, Analytics, and Client Management
Sub-features: Admin calendar view, revenue analytics dashboard, client directory

UC-3.3.1: View the Booking Calendar
Actor: Tenant Admin, Manager, or Staff
Preconditions:

The actor is logged in.

Main Flow:

The actor navigates to Calendar.
The system displays a calendar defaulting to the current week in a day-column grid view.
Each booking appears as a color-coded block at its scheduled time. The block color corresponds to the service's configured color code.
The actor switches between day view, week view, and month view using view tabs.
The actor filters the calendar by staff member or service using filter controls.
The actor clicks a booking block to open a summary popup with options to view full details, reschedule, or cancel.
Staff users see only their own assigned bookings. Managers and Admins see all bookings under the tenant.

Postconditions:

The actor has a real-time view of the booking schedule within their permission scope.


UC-3.3.2: View Revenue Analytics Dashboard
Actor: Tenant Admin or Manager
Preconditions:

The actor is logged in with at least Manager role.

Main Flow:

The actor navigates to Dashboard.
The system displays the following metrics for the current period (defaulting to the current month, with a period selector for custom date ranges):

Total revenue collected.
Total number of bookings.
Bookings by status breakdown: confirmed, pending, completed, cancelled, no-show.
Revenue breakdown by service (bar or pie chart).
Booking trend over time (line chart).
Today's upcoming schedule: a sorted list of today's bookings with time, customer name, service, and specialist.

The actor selects a different period using the date range filter. The system reloads all metrics for the selected range.
Loading states display skeleton placeholders while data is fetched.

Postconditions:

The actor has a real-time view of business performance metrics scoped to their tenant only.


UC-3.3.3: View and Search the Client Directory
Actor: Tenant Admin or Manager
Preconditions:

The actor is logged in with at least Manager role.
At least one booking has been placed under the tenant.

Main Flow:

The actor navigates to Clients.
The system displays a paginated list of all unique clients who have ever placed a booking under this tenant. Each entry shows: client name, email address, phone number, total number of bookings, total amount spent, and last visit date.
The actor searches the list by client name or email using the search input. Results update as the actor types.
The actor selects a client to open their detail view.
The system displays the client's contact information and their complete booking history under this tenant.

Postconditions:

The actor has a complete view of the tenant's client base within their permission scope.
No data from other tenants is accessible.


MODULE 4: PAYMENTS AND BILLING
Feature 4.1: Payment Collection
Sub-features: Online payment at booking, payment status tracking, refund processing

UC-4.1.1: Customer Pays at Booking
Actor: End Customer (Guest or Authenticated)
Preconditions:

The selected service has a price greater than zero.
The tenant has a payment gateway configured.

Main Flow:

After the customer confirms their contact details (UC-3.2.1, step 10), the system displays a payment step before creating the booking.
The system shows the total amount due, service name, and accepted payment methods.
The customer enters their payment details.
The customer submits payment.
The system sends the payment request to the configured payment gateway.
The gateway processes the payment and returns a success or failure response.
On success: the system records the transaction with status Paid, links it to the booking, and proceeds to create the booking record (UC-3.2.1, step 12 onward).
On failure: the system displays an error message with the reason and prompts the customer to retry or use a different method.

Alternate Flow: Free Service

If the service price is zero, the payment step is skipped entirely and the booking is created immediately after contact details are submitted.

Postconditions:

A payment transaction record exists and is linked to the booking.
The booking is created only after successful payment for paid services.


UC-4.1.2: Process a Refund
Actor: Tenant Admin or Manager
Preconditions:

A paid booking has been cancelled and the cancellation policy permits a full or partial refund.

Main Flow:

The system automatically creates a refund record with status Pending when a paid booking is cancelled and the policy permits a refund.
The Tenant Admin or Manager reviews the refund queue under Payments > Refunds.
The actor selects the pending refund and clicks Process Refund.
The system sends a refund request to the payment gateway for the eligible amount.
The gateway processes the refund and returns confirmation.
The system updates the refund record to Completed and updates the original transaction to reflect the refund.
The system sends a refund confirmation email to the customer with the refund amount and expected processing time.

Alternate Flow: Refund Rejected by Gateway

The system sets the refund status to Failed and alerts the Tenant Admin.
The Tenant Admin contacts the customer manually to resolve.

Postconditions:

The refund is recorded and the customer receives confirmation.


Feature 4.2: Tenant Billing (Platform Subscription)
Sub-features: Subscription plan management, invoice generation, payment failure handling

UC-4.2.1: Tenant Subscribes to a Plan
Actor: Tenant Admin
Preconditions:

The tenant is active.
The tenant has not yet subscribed or is changing their plan.

Main Flow:

The Tenant Admin navigates to Billing > Plans.
The system displays available subscription plan tiers (e.g., Professional and Enterprise) with pricing, billing cycle (monthly or annual), and feature limits such as maximum resources, maximum bookings per month, and number of staff users.
The Tenant Admin selects a plan and clicks Subscribe.
The system displays a checkout form requesting billing email and payment details.
The Tenant Admin submits the form.
The system sends the subscription request to the platform's billing provider.
The billing provider creates a recurring subscription and charges the first billing cycle.
On success, the system sets the tenant's subscription status to Active and records the plan details.
The system sends an invoice to the Tenant Admin's billing email.

Postconditions:

The tenant has an active subscription.
Platform features are unlocked according to the plan tier.


UC-4.2.2: Handle Failed Subscription Payment
Actor: System (automated), Tenant Admin
Preconditions:

The tenant has an active subscription.
A scheduled payment attempt fails.

Main Flow:

The billing provider notifies the system of a failed payment via webhook.
The system sets the tenant's subscription status to Past Due.
The system sends an email to the Tenant Admin with the failure reason and a link to update payment details.
The system retries the payment after 3 days and again after 7 days.
If payment succeeds on retry, the subscription status returns to Active and the Tenant Admin is notified.
If all retries fail after 14 days, the system sets the subscription status to Suspended and restricts access to paid features.
The tenant's existing data is preserved but new bookings are blocked.

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
The Tenant Admin selects a gateway and clicks Configure.
The system displays a form requesting the gateway's API credentials (publishable key and secret key) and mode: Test or Live.
The Tenant Admin enters the credentials and saves.
The system encrypts and stores the credentials scoped to this tenant only.
The system runs a validation request to the gateway using the provided credentials to confirm they are valid.
If valid, the system marks the gateway as Connected and enables payment collection on the tenant's public booking pages.
If invalid, the system displays an error and prompts the Tenant Admin to recheck their credentials.

Postconditions:

The tenant's booking flow accepts payments through the configured gateway.
Credentials are stored encrypted and never exposed in plaintext to any user.


CROSS-CUTTING CONCERNS
Feature 5.1: Notifications
UC-5.1.1: Send Automated Notifications
Actor: System
Triggers and notification rules:

Booking Confirmed (Guest): system sends email to the guest's provided email address containing: booking reference, business name, service name, specialist name, date, time, duration, total amount, cancellation policy, and a secure cancellation link. This link allows the guest to cancel without creating an account.
Booking Confirmed (Authenticated Customer): system sends email to the customer's registered email containing: booking reference, service details, date, time, and a link to their customer portal.
Booking Pending: system sends email to the customer or guest stating the booking is awaiting staff confirmation. For guests, the message states they will be notified by email when the outcome is decided.
Booking Confirmed by Admin (Manual Mode): system sends email to the customer or guest confirming the booking is now confirmed. For guests, the email includes the secure cancellation link.
Booking Rejected by Admin: system sends email to the customer or guest with the rejection reason if one was provided.
Booking Cancelled by Customer or Guest: system sends cancellation confirmation to the customer or guest email address, and sends an alert to the assigned staff member and admin.
Booking Cancelled by Staff or Admin: system sends a cancellation notification to the customer or guest email with the reason if provided.
Booking Rescheduled: system sends an updated confirmation email to the customer or guest. For guests, the updated email includes a new secure cancellation link reflecting the new appointment time.
Booking Reminder: system sends a reminder email to the customer or guest 24 hours before the scheduled appointment. Reminder timing is configurable per tenant. For guests, the reminder email includes the cancellation link.
New Booking Alert to Staff: system sends an alert to the assigned staff member and the tenant admin when any new booking is placed.
Refund Processed: system sends email to the customer or guest confirming the refund amount and expected processing time.

All notification email content is scoped to the tenant and uses the tenant's business name, logo, theme color, and contact details in the email template.

Feature 5.2: Audit Logging
UC-5.2.1: Record System Events
Actor: System (automated)
Main Flow:

Every significant action in the system generates an audit log entry.
Each entry records: timestamp, actor user ID (or "guest" for unauthenticated booking actions), actor role, tenant ID, action type (e.g., booking.created, booking.cancelled, user.role.changed, tenant.suspended), affected record ID, and IP address.
Audit logs are immutable. No user, including Super Admin, can delete or edit them.
Super Admin accesses audit logs for any tenant via the Super Admin panel.
Tenant Admin accesses audit logs scoped only to their tenant via Settings > Audit Log.
Logs are retained for a minimum of 90 days and are exportable as CSV.

Postconditions:

Every user action and system event is traceable.
Compliance and debugging are supported by a full audit trail.
