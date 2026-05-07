Entity Relationship Diagram: Multitenant Booking System

DESIGN PRINCIPLES
Every table carries a tenant_id foreign key except for the three platform-level tables: tenants, subscription_plans, and super_admins. This enforces row-level multitenancy. No query across any tenant-scoped table runs without a tenant_id filter.
Aggregate roots own their consistency boundaries. Child entities do not exist without their aggregate root. Deletions cascade from root to children.

AGGREGATE ROOTS AND THEIR CHILD ENTITIES

AGGREGATE ROOT 1: Tenant
The central entity of the entire system. Every other tenant-scoped entity traces back to this record.
Table: tenants
ColumnTypeConstraintsidUUIDPKslugVARCHAR(50)UNIQUE, NOT NULLbusiness_nameVARCHAR(255)NOT NULLowner_full_nameVARCHAR(255)NOT NULLowner_emailVARCHAR(255)UNIQUE, NOT NULLowner_phoneVARCHAR(50)statusENUMNOT NULL: pending, active, suspended, deactivatedemail_verified_atTIMESTAMPNULLABLEcreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULLdeactivated_atTIMESTAMPNULLABLE
Relationships:

Has one tenant_profile (child)
Has one tenant_settings (child)
Has one tenant_subscription (child)
Has many users
Has many resources
Has many services
Has many bookings
Has many payment_transactions
Has many audit_logs
Has many notification_templates


Child Entity 1.1: tenant_profiles
Stores the business-facing public profile.
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, UNIQUE, NOT NULLlogo_urlVARCHAR(500)NULLABLEaddress_line_1VARCHAR(255)NULLABLEaddress_line_2VARCHAR(255)NULLABLEcityVARCHAR(100)NULLABLEstateVARCHAR(100)NULLABLEpostal_codeVARCHAR(20)NULLABLEcountry_codeCHAR(2)NULLABLE, ISO 3166-1 alpha-2timezoneVARCHAR(100)NOT NULL, IANA timezone stringcurrency_codeCHAR(3)NOT NULL, ISO 4217website_urlVARCHAR(500)NULLABLEcontact_emailVARCHAR(255)NULLABLEcontact_phoneVARCHAR(50)NULLABLEdate_formatVARCHAR(20)NOT NULL, default: YYYY-MM-DDtime_formatENUMNOT NULL: 12h, 24hlanguage_codeVARCHAR(10)NOT NULL, default: encreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL

Child Entity 1.2: tenant_settings
Stores operational configuration for the tenant.
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, UNIQUE, NOT NULLbooking_confirmation_modeENUMNOT NULL: automatic, manualmin_advance_booking_hoursINTNOT NULL, default: 1max_advance_booking_daysINTNOT NULL, default: 60cancellation_cutoff_hoursINTNOT NULL, default: 24late_cancellation_policyENUMNOT NULL: no_refund, partial_refund, full_refundguest_booking_enabledBOOLEANNOT NULL, default: falsenotify_booking_confirmedBOOLEANNOT NULL, default: truenotify_booking_cancelledBOOLEANNOT NULL, default: truenotify_booking_reminderBOOLEANNOT NULL, default: truenotify_new_customerBOOLEANNOT NULL, default: falsereminder_hours_beforeINTNOT NULL, default: 24created_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL

Child Entity 1.3: tenant_subscriptions
Tracks the tenant's current platform subscription.
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, UNIQUE, NOT NULLsubscription_plan_idUUIDFK subscription_plans.id, NOT NULLbilling_provider_subscription_idVARCHAR(255)UNIQUE, NOT NULLstatusENUMNOT NULL: active, past_due, suspended, cancelledbilling_emailVARCHAR(255)NOT NULLbilling_cycleENUMNOT NULL: monthly, annualcurrent_period_startDATENOT NULLcurrent_period_endDATENOT NULLcancelled_atTIMESTAMPNULLABLEcreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL

Child Entity 1.4: tenant_invoices
One invoice per billing cycle.
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLtenant_subscription_idUUIDFK tenant_subscriptions.id, NOT NULLinvoice_numberVARCHAR(50)UNIQUE, NOT NULLbilling_period_startDATENOT NULLbilling_period_endDATENOT NULLsubtotal_amountDECIMAL(12,2)NOT NULLtax_amountDECIMAL(12,2)NOT NULL, default: 0total_amountDECIMAL(12,2)NOT NULLcurrency_codeCHAR(3)NOT NULLstatusENUMNOT NULL: pending, paid, failedpaid_atTIMESTAMPNULLABLEpdf_urlVARCHAR(500)NULLABLEcreated_atTIMESTAMPNOT NULL

Child Entity 1.5: tenant_payment_gateways
Stores gateway configuration per tenant.
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLgateway_providerENUMNOT NULL: stripe, paypal, paymongopublishable_keyVARCHAR(500)NOT NULLsecret_key_encryptedTEXTNOT NULL, encrypted at restmodeENUMNOT NULL: test, liveis_activeBOOLEANNOT NULL, default: falsevalidated_atTIMESTAMPNULLABLEcreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL
Constraint: only one active gateway per tenant at a time. Enforced via partial unique index on (tenant_id, is_active) where is_active = true.

AGGREGATE ROOT 2: User (Extended IdentityUser<Guid>)
Covers all user types: Tenant Admin, Manager, Staff, and Customer. All scoped to a tenant.
Table: users (AspNetUsers merged via EF Identity mapping)
ColumnTypeConstraintsidUUIDPK (IdentityUser<Guid>)tenant_idUUIDFK tenants.id, NOT NULLfull_nameVARCHAR(255)NOT NULLemailVARCHAR(255)NOT NULLphoneVARCHAR(50)NULLABLEpassword_hashVARCHAR(255)NULLABLE (managed by Identity, not in ERD)roleENUMNOT NULL: tenant_admin, manager, staff, customerstatusENUMNOT NULL: invited, active, inactiveemail_verified_atTIMESTAMPNULLABLElast_login_atTIMESTAMPNULLABLEcreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL
Unique constraint: (tenant_id, email). The same email address can exist across different tenants but not twice within the same tenant.
Identity-Managed Tables (added by ASP.NET Core Identity):
- AspNetRoles: role definitions (tenant_admin, manager, staff, customer)
- AspNetUserRoles: user-to-role assignments
- AspNetUserTokens: password reset, invitation, email confirmation tokens
- AspNetUserClaims: custom claims per user
- AspNetRoleClaims: custom claims per role
- AspNetUserLogins: external login providers
Relationships:

Belongs to one tenant
Has one user_profile (child)
Has many user_resource_assignments (child, for staff users)
Has many bookings (as customer)
Has many audit_logs


Child Entity 2.1: user_profiles
Optional extended profile information.
ColumnTypeConstraintsidUUIDPKuser_idUUIDFK users.id, UNIQUE, NOT NULLtenant_idUUIDFK tenants.id, NOT NULLdate_of_birthDATENULLABLEgenderVARCHAR(50)NULLABLEnotesTEXTNULLABLE (admin-only notes about the customer)marketing_opt_inBOOLEANNOT NULL, default: falsecreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL

Child Entity 2.2: user_resource_assignments
Links staff users to the specific resources they manage.
ColumnTypeConstraintsidUUIDPKuser_idUUIDFK users.id, NOT NULLresource_idUUIDFK resources.id, NOT NULLtenant_idUUIDFK tenants.id, NOT NULLcreated_atTIMESTAMPNOT NULL
Unique constraint: (user_id, resource_id).

Child Entity 2.3: password_reset_tokens
ColumnTypeConstraintsidUUIDPKuser_idUUIDFK users.id, NOT NULLtenant_idUUIDFK tenants.id, NOT NULLtoken_hashVARCHAR(255)NOT NULLexpires_atTIMESTAMPNOT NULLused_atTIMESTAMPNULLABLEcreated_atTIMESTAMPNOT NULL

AGGREGATE ROOT 3: Resource
Represents any bookable entity: a person, room, equipment, or slot pool.
Table: resources
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLnameVARCHAR(255)NOT NULLdescriptionTEXTNULLABLEresource_typeENUMNOT NULL: person, room, equipment, slot_basedcapacityINTNOT NULL, default: 1location_idUUIDFK locations.id, NULLABLEis_activeBOOLEANNOT NULL, default: truecreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL
Relationships:

Belongs to one tenant
Has many resource_availability_schedules (child)
Has many resource_availability_exceptions (child)
Has many service_resources (child, join to services)
Has many user_resource_assignments (child)
Has many booking_slots (derived, not stored)


Child Entity 3.1: resource_availability_schedules
Defines the recurring weekly working hours for a resource.
ColumnTypeConstraintsidUUIDPKresource_idUUIDFK resources.id, NOT NULLtenant_idUUIDFK tenants.id, NOT NULLday_of_weekSMALLINTNOT NULL, 0=Sunday through 6=Saturdayis_availableBOOLEANNOT NULL, default: truestart_timeTIMENULLABLE, required if is_available = trueend_timeTIMENULLABLE, required if is_available = truecreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL
Unique constraint: (resource_id, day_of_week).

Child Entity 3.2: resource_break_periods
Defines recurring breaks within a working day for a resource.
ColumnTypeConstraintsidUUIDPKresource_availability_schedule_idUUIDFK resource_availability_schedules.id, NOT NULLtenant_idUUIDFK tenants.id, NOT NULLresource_idUUIDFK resources.id, NOT NULLbreak_start_timeTIMENOT NULLbreak_end_timeTIMENOT NULLlabelVARCHAR(100)NULLABLE, e.g. Lunch Breakcreated_atTIMESTAMPNOT NULL

Child Entity 3.3: resource_availability_exceptions
Overrides the base schedule for specific dates.
ColumnTypeConstraintsidUUIDPKresource_idUUIDFK resources.id, NOT NULLtenant_idUUIDFK tenants.id, NOT NULLexception_dateDATENOT NULLexception_typeENUMNOT NULL: unavailable_all_day, unavailable_hours, available_extra_hoursstart_timeTIMENULLABLEend_timeTIMENULLABLEnoteTEXTNULLABLEcreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL
Unique constraint: (resource_id, exception_date, exception_type).

AGGREGATE ROOT 4: Service
Defines what can be booked: duration, price, buffer, and rules.
Table: services
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLnameVARCHAR(255)NOT NULLdescriptionTEXTNULLABLEduration_minutesINTNOT NULLbuffer_before_minutesINTNOT NULL, default: 0buffer_after_minutesINTNOT NULL, default: 0priceDECIMAL(12,2)NOT NULL, default: 0.00currency_codeCHAR(3)NOT NULLmin_advance_booking_hoursINTNULLABLE, overrides tenant setting if setmax_advance_booking_daysINTNULLABLE, overrides tenant setting if setcancellation_policy_overrideENUMNULLABLE: no_refund, partial_refund, full_refundis_activeBOOLEANNOT NULL, default: truecreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL
Relationships:

Belongs to one tenant
Has many service_resources (child, join to resources)
Has many bookings


Child Entity 4.1: service_resources
Join table linking services to their assignable resources.
ColumnTypeConstraintsidUUIDPKservice_idUUIDFK services.id, NOT NULLresource_idUUIDFK resources.id, NOT NULLtenant_idUUIDFK tenants.id, NOT NULLcreated_atTIMESTAMPNOT NULL
Unique constraint: (service_id, resource_id).

AGGREGATE ROOT 5: Booking
The central transactional entity. Represents one confirmed or pending reservation.
Table: bookings
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLbooking_referenceVARCHAR(20)UNIQUE, NOT NULL, e.g. BK-2026-00842service_idUUIDFK services.id, NOT NULLresource_idUUIDFK resources.id, NOT NULLuser_idUUIDFK users.id, NULLABLE, null if guest bookingguest_nameVARCHAR(255)NULLABLE, populated for guest bookingsguest_emailVARCHAR(255)NULLABLEguest_phoneVARCHAR(50)NULLABLEscheduled_dateDATENOT NULLscheduled_start_timeTIMENOT NULLscheduled_end_timeTIMENOT NULLduration_minutesINTNOT NULL, snapshot of service duration at booking timestatusENUMNOT NULL: pending, confirmed, cancelled, completed, no_showconfirmation_modeENUMNOT NULL: automatic, manual, snapshot of tenant settingprice_snapshotDECIMAL(12,2)NOT NULL, price at time of bookingcurrency_codeCHAR(3)NOT NULLcustomer_notesTEXTNULLABLEcancellation_reasonTEXTNULLABLEcancelled_atTIMESTAMPNULLABLEcancelled_by_user_idUUIDFK users.id, NULLABLErescheduled_from_booking_idUUIDFK bookings.id, NULLABLE, self-referencecreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL
Index: (tenant_id, resource_id, scheduled_date, status) for slot availability queries.
Index: (tenant_id, user_id) for customer booking history queries.
Index: (tenant_id, scheduled_date) for calendar queries.
Relationships:

Belongs to one tenant
Belongs to one service
Belongs to one resource
Belongs to one user (or guest)
Has one payment_transaction (child)
Has one refund (child)
Has many booking_status_logs (child)
Has many notifications (child)


Child Entity 5.1: booking_status_logs
Immutable history of every status change on a booking.
ColumnTypeConstraintsidUUIDPKbooking_idUUIDFK bookings.id, NOT NULLtenant_idUUIDFK tenants.id, NOT NULLprevious_statusENUMNULLABLEnew_statusENUMNOT NULLchanged_by_user_idUUIDFK users.id, NULLABLEchange_reasonTEXTNULLABLEcreated_atTIMESTAMPNOT NULL

AGGREGATE ROOT 6: PaymentTransaction
Tracks the payment for one booking. One booking has at most one primary transaction.
Table: payment_transactions
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLbooking_idUUIDFK bookings.id, UNIQUE, NOT NULLgateway_providerENUMNOT NULL: stripe, paypal, paymongogateway_transaction_idVARCHAR(255)UNIQUE, NOT NULLamountDECIMAL(12,2)NOT NULLcurrency_codeCHAR(3)NOT NULLstatusENUMNOT NULL: pending, paid, failed, refunded, partially_refundedpayment_method_typeVARCHAR(50)NULLABLE, e.g. credit_card, debit_cardpayment_method_last4CHAR(4)NULLABLEpaid_atTIMESTAMPNULLABLEfailure_reasonTEXTNULLABLEcreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL
Relationships:

Belongs to one tenant
Belongs to one booking
Has one refund (child)


Child Entity 6.1: refunds
Tracks the refund tied to a payment transaction.
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLpayment_transaction_idUUIDFK payment_transactions.id, UNIQUE, NOT NULLbooking_idUUIDFK bookings.id, NOT NULLgateway_refund_idVARCHAR(255)NULLABLE, populated after gateway confirmsrefund_amountDECIMAL(12,2)NOT NULLcurrency_codeCHAR(3)NOT NULLstatusENUMNOT NULL: pending, completed, failedrefund_typeENUMNOT NULL: full, partialreasonTEXTNULLABLEprocessed_by_user_idUUIDFK users.id, NULLABLEprocessed_atTIMESTAMPNULLABLEcreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL

AGGREGATE ROOT 7: AuditLog
Immutable system-wide event record. No updates or deletes permitted on this table.
Table: audit_logs
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NULLABLE, null for platform-level eventsactor_user_idUUIDFK users.id, NULLABLE, null for system-triggered eventsactor_roleENUMNULLABLE: super_admin, tenant_admin, manager, staff, customer, systemaction_typeVARCHAR(100)NOT NULL, e.g. booking.created, user.role.changedentity_typeVARCHAR(100)NOT NULL, e.g. booking, user, tenantentity_idUUIDNOT NULLprevious_stateJSONBNULLABLE, snapshot of record before changenew_stateJSONBNULLABLE, snapshot of record after changeip_addressVARCHAR(45)NULLABLEuser_agentTEXTNULLABLEcreated_atTIMESTAMPNOT NULL
Index: (tenant_id, created_at) for tenant audit log queries.
Index: (entity_type, entity_id) for entity-level history queries.

AGGREGATE ROOT 8: NotificationLog
Records every notification dispatched by the system.
Table: notification_logs
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLbooking_idUUIDFK bookings.id, NULLABLEuser_idUUIDFK users.id, NULLABLErecipient_emailVARCHAR(255)NOT NULLnotification_typeENUMNOT NULL: booking_confirmed, booking_pending, booking_confirmed_by_admin, booking_rejected, booking_cancelled_by_customer, booking_cancelled_by_staff, booking_rescheduled, booking_reminder, new_booking_alert, refund_confirmed, subscription_invoice, subscription_past_due, subscription_suspendedchannelENUMNOT NULL: emailstatusENUMNOT NULL: queued, sent, failedsent_atTIMESTAMPNULLABLEfailure_reasonTEXTNULLABLEcreated_atTIMESTAMPNOT NULL

PLATFORM-LEVEL TABLES (No tenant_id)

Platform Table 1: super_admins
Separate from the users table entirely. Super admins are platform operators, not tenants.
Table: super_admins
ColumnTypeConstraintsidUUIDPKfull_nameVARCHAR(255)NOT NULLemailVARCHAR(255)UNIQUE, NOT NULLpassword_hashVARCHAR(255)NOT NULLis_activeBOOLEANNOT NULL, default: truelast_login_atTIMESTAMPNULLABLEcreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL

Platform Table 2: subscription_plans
Defines the available platform pricing tiers.
Table: subscription_plans
ColumnTypeConstraintsidUUIDPKnameVARCHAR(100)NOT NULL, e.g. Starter, Growth, Enterprisebilling_cycleENUMNOT NULL: monthly, annualpriceDECIMAL(12,2)NOT NULLcurrency_codeCHAR(3)NOT NULL, default: USDmax_resourcesINTNULLABLE, null means unlimitedmax_bookings_per_monthINTNULLABLE, null means unlimitedmax_staff_usersINTNULLABLE, null means unlimitedis_activeBOOLEANNOT NULL, default: truecreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL

Platform Table 3: locations
Optional. Used when a tenant operates multiple physical locations.
Table: locations
ColumnTypeConstraintsidUUIDPKtenant_idUUIDFK tenants.id, NOT NULLnameVARCHAR(255)NOT NULLaddress_line_1VARCHAR(255)NULLABLEaddress_line_2VARCHAR(255)NULLABLEcityVARCHAR(100)NULLABLEstateVARCHAR(100)NULLABLEpostal_codeVARCHAR(20)NULLABLEcountry_codeCHAR(2)NULLABLEis_activeBOOLEANNOT NULL, default: truecreated_atTIMESTAMPNOT NULLupdated_atTIMESTAMPNOT NULL

ENTITY CLASSIFICATION SUMMARY
Aggregate Roots:

Tenant: owns the entire tenant workspace boundary.
User: owns identity, role, and access within a tenant.
Resource: owns availability schedules and exception rules.
Service: owns duration, pricing, and booking rules.
Booking: owns the reservation lifecycle and its history.
PaymentTransaction: owns the payment and refund lifecycle for one booking.
AuditLog: owns the immutable event record.
NotificationLog: owns the dispatched notification record.

Child Entities:

tenant_profiles: child of Tenant
tenant_settings: child of Tenant
tenant_subscriptions: child of Tenant
tenant_invoices: child of Tenant via tenant_subscriptions
tenant_payment_gateways: child of Tenant
user_profiles: child of User
user_resource_assignments: child of User and Resource
password_reset_tokens: child of User
resource_availability_schedules: child of Resource
resource_break_periods: child of resource_availability_schedules
resource_availability_exceptions: child of Resource
service_resources: child of Service and Resource
booking_status_logs: child of Booking
refunds: child of PaymentTransaction

Platform Tables (no aggregate root, no tenant scope):

super_admins
subscription_plans
locations


KEY RELATIONSHIP SUMMARY

tenants 1:1 tenant_profiles
tenants 1:1 tenant_settings
tenants 1:1 tenant_subscriptions
tenant_subscriptions 1:N tenant_invoices
tenants 1:N tenant_payment_gateways
tenants 1:N users
users 1:1 user_profiles
users 1:N user_resource_assignments
tenants 1:N resources
resources 1:N resource_availability_schedules
resource_availability_schedules 1:N resource_break_periods
resources 1:N resource_availability_exceptions
tenants 1:N services
services N:N resources (via service_resources)
tenants 1:N bookings
bookings N:1 services
bookings N:1 resources
bookings N:1 users
bookings 1:N booking_status_logs
bookings 1:1 payment_transactions
payment_transactions 1:1 refunds
bookings 1:N notification_logs
subscription_plans 1:N tenant_subscriptions