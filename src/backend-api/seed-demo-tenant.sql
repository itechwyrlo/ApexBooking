/*
================================================================================
Demo tenant seed script — ApexBooking (SQL Server)
================================================================================
Reproduces the end-state of the real "tenant request -> approved -> owner
invited -> invite redeemed" flow (see ProvisionTenantOnRequestApprovedHandler,
SendOwnerSetupInvitationOnTenantCreatedHandler, ResetPasswordCommandHandler /
ApplicationUserService.ResetPasswordAsync), a second staff account mirroring
AddTeamHandler's "invite a team member" flow, and a full barbershop service
catalog (Tenant.CreateService / Tenant.AssignStaffToService) — all WITHOUT
going through the app.

WHY THIS CAN'T LITERALLY REPLAY THE INVITE: ApexBooking has no persisted
Invitation / PasswordResetToken table — invite & reset links are stateless
ASP.NET Core Identity DataProtector tokens (72h lifespan, configured in
AuthenticationExtensions.cs), never written to the DB. So instead of
inserting a "redeemable token" this script inserts accounts already in the
POST-REDEMPTION state: EmailConfirmed = 1, IsActive = 1, a real password
hash, exactly as ApplicationUserService.ResetPasswordAsync leaves them.

Both demo accounts log in with password:  Demo@12345
(meets the app's Identity policy: 8+ chars, upper, lower, digit, symbol)

PasswordHash below is a genuine ASP.NET Core Identity v3 hash
(PasswordHasher<T>, PBKDF2-HMAC-SHA256), generated with the same hasher
class the app uses and round-trip verified via VerifyHashedPassword before
being pasted in here — not a placeholder.

RERUNNABLE: the script starts by deleting any prior demo tenant (matched by
slug 'demo-salon' or 'demo-barbershop', in FK-safe order) so it can be run
repeatedly while iterating.

Demo data created:
  - 1 tenant "Demo Barbershop Co." (slug: demo-barbershop), business_type =
    BarberShop, fully provisioned (branch, booking_policy, payment_policy —
    same defaults Tenant.Create seeds for a real approved request)
  - 1 branch "Main Branch" with real operating hours: Mon-Sat 9:00-19:00,
    Sun closed
  - 2 ApplicationUsers: an Owner and a Staff (barber), both confirmed/active
  - 2 TenantMembers rows linking them to the tenant with matching weekly
    schedules (same hours as the branch, so both are actually bookable)
  - 6 barbershop services (services table), all priced in PHP
  - MemberServices rows assigning BOTH staff to ALL services, mirroring
    Tenant.AssignStaffToService, so every service is bookable with either
    team member in the UI

Run this whole script in one batch against the ApexBooking database.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRANSACTION;

BEGIN TRY

    ----------------------------------------------------------------------
    -- 0. Cleanup — remove any prior run of this demo tenant so the script
    --    is safely rerunnable. Deletes in FK-safe (child-first) order.
    ----------------------------------------------------------------------
    DECLARE @ExistingTenantId UNIQUEIDENTIFIER =
        (SELECT TOP 1 tenant_id FROM tenant WHERE slug IN (N'demo-salon', N'demo-barbershop'));

    IF @ExistingTenantId IS NOT NULL
    BEGIN
        DELETE FROM TenantMemberWeeklySchedules
        WHERE TenantMemberId IN (SELECT TenantMemberId FROM TenantMembers WHERE TenantId = @ExistingTenantId);

        DELETE FROM MemberServices
        WHERE TenantMemberId IN (SELECT TenantMemberId FROM TenantMembers WHERE TenantId = @ExistingTenantId)
           OR ServiceId IN (SELECT id FROM services WHERE tenant_id = @ExistingTenantId);

        DELETE FROM services WHERE tenant_id = @ExistingTenantId;

        DELETE FROM ApplicationUsers
        WHERE Id IN (SELECT UserId FROM TenantMembers WHERE TenantId = @ExistingTenantId AND UserId IS NOT NULL);

        DELETE FROM TenantMembers WHERE TenantId = @ExistingTenantId;

        DELETE FROM BranchOperatingHours
        WHERE BranchId IN (SELECT branch_id FROM branches WHERE tenant_id = @ExistingTenantId);

        DELETE FROM branches WHERE tenant_id = @ExistingTenantId;
        DELETE FROM booking_policy WHERE tenant_id = @ExistingTenantId;
        DELETE FROM payment_policy WHERE tenant_id = @ExistingTenantId;
        DELETE FROM tenant WHERE tenant_id = @ExistingTenantId;
    END

    DECLARE @UtcNow            DATETIME2 = SYSUTCDATETIME();

    -- Tenant / branch
    DECLARE @TenantId          UNIQUEIDENTIFIER = NEWID();
    DECLARE @BranchId          UNIQUEIDENTIFIER = NEWID();
    DECLARE @BookingPolicyId   UNIQUEIDENTIFIER = NEWID();
    DECLARE @PaymentPolicyId   UNIQUEIDENTIFIER = NEWID();

    -- Owner account
    DECLARE @OwnerUserId        UNIQUEIDENTIFIER = NEWID();
    DECLARE @OwnerTenantMemberId UNIQUEIDENTIFIER = NEWID();
    DECLARE @OwnerEmail          NVARCHAR(256) = N'owner@demo.apexbooking.test';
    DECLARE @OwnerFirstName      NVARCHAR(100) = N'Ava';
    DECLARE @OwnerLastName       NVARCHAR(100) = N'Reyes';
    DECLARE @OwnerPasswordHash   NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEFB/E6d6jFMdl2hqy0xXZJKlyukO0pstAWlcgkJoUvLTLYiKOfxL6QzwpPQlfcs69g==';
    DECLARE @OwnerSecurityStamp  NVARCHAR(MAX) = N'2D6ED0EC-9398-4E65-998D-D23FF9F33494';
    DECLARE @OwnerConcurrencyStamp NVARCHAR(MAX) = N'7c65b020-625c-4b36-8cf8-08ed3b12fd00';

    -- Staff account (mirrors AddTeamHandler's staff-invite flow)
    DECLARE @StaffUserId        UNIQUEIDENTIFIER = NEWID();
    DECLARE @StaffTenantMemberId UNIQUEIDENTIFIER = NEWID();
    DECLARE @StaffEmail          NVARCHAR(256) = N'staff@demo.apexbooking.test';
    DECLARE @StaffFirstName      NVARCHAR(100) = N'Liam';
    DECLARE @StaffLastName       NVARCHAR(100) = N'Cruz';
    DECLARE @StaffPasswordHash   NVARCHAR(MAX) = N'AQAAAAIAAYagAAAAEDs/2934G/XUGpTHRYJBBhK9Gv9MFUfV784J65nqPtpsdGQP05AnUPyvV+vkUEw26A==';
    DECLARE @StaffSecurityStamp  NVARCHAR(MAX) = N'67C7899E-2718-4ECE-B35A-8C47D2CF860E';
    DECLARE @StaffConcurrencyStamp NVARCHAR(MAX) = N'1cfedbab-1dd7-45d8-aab0-b395facad628';

    ----------------------------------------------------------------------
    -- 1. tenant  (Tenant.Create defaults: is_active=1, setup_required=1,
    --    setup_completed=0, theme_palette_id='indigo', dark_mode=0)
    ----------------------------------------------------------------------
    INSERT INTO tenant
        (tenant_id, slug, owner_first_name, owner_last_name, owner_email, owner_phone_number,
         [plan], is_active, setup_required, setup_completed,
         trial_started_at, trial_ended_at, trial_expired_at, trial_reminder_sent_at,
         business_name, logo_url, business_type, description, contact_phone_number,
         theme_palette_id, public_page_dark_mode, created_at, updated_at, deactivated_at)
    VALUES
        (@TenantId, N'demo-barbershop', @OwnerFirstName, @OwnerLastName, @OwnerEmail, N'+639171234567',
         N'Professional', 1, 1, 0,
         NULL, NULL, NULL, NULL,
         N'Demo Barbershop Co.', NULL, N'BarberShop', N'A demo barbershop tenant seeded directly via SQL for local/test login.', N'+639171234567',
         N'indigo', 0, @UtcNow, @UtcNow, NULL);

    ----------------------------------------------------------------------
    -- 2. branches  (Tenant.AddBranch("Main Branch", "Asia/Manila", ...))
    ----------------------------------------------------------------------
    INSERT INTO branches
        (branch_id, tenant_id, branch_name, street, barangay, city_municipality, province,
         zip_code, country, time_zone_id, is_active, created_at, updated_at)
    VALUES
        (@BranchId, @TenantId, N'Main Branch', N'123 Rizal Avenue', N'Poblacion', N'Makati City', N'Metro Manila',
         N'1200', N'Philippines', N'Asia/Manila', 1, @UtcNow, @UtcNow);

    -- Real operating hours: Mon-Sat 09:00-19:00, Sun closed
    INSERT INTO BranchOperatingHours (DayOfWeek, BranchId, StartTime, EndTime, IsOff)
    SELECT d.DayOfWeek, @BranchId, d.StartTime, d.EndTime, d.IsOff
    FROM (VALUES
        (N'Sunday',    CAST('00:00:00' AS TIME), CAST('00:00:00' AS TIME), CAST(1 AS BIT)),
        (N'Monday',    CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Tuesday',   CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Wednesday', CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Thursday',  CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Friday',    CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Saturday',  CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT))
    ) AS d(DayOfWeek, StartTime, EndTime, IsOff);

    ----------------------------------------------------------------------
    -- 3. booking_policy / payment_policy  (BookingPolicy/PaymentPolicy.CreateDefault)
    ----------------------------------------------------------------------
    -- min_advance_booking_hours = 0 (not the real default of 48 — see BookingPolicy.CreateDefault)
    -- so every seeded service is same-day bookable for demo purposes without extra clicking.
    INSERT INTO booking_policy
        (booking_policy_id, tenant_id, booking_confirmation_mode, min_advance_booking_hours,
         max_advance_booking_days, cancellation_cutoff_hours, late_cancellation_policy,
         notify_booking_confirmed, notify_booking_cancelled, notify_booking_reminder,
         notify_new_customer, reminder_hours_before, created_at, updated_at)
    VALUES
        (@BookingPolicyId, @TenantId, N'Automatic', 0, 60, 24, N'NoRefund', 1, 1, 1, 1, 24, @UtcNow, @UtcNow);

    INSERT INTO payment_policy
        (payment_policy_id, tenant_id, requirement_type, deposit_type, deposit_value,
         on_time_refund_percent, late_cancellation_refund_percent, refund_enabled,
         refund_review_deadline_days, created_at, updated_at)
    VALUES
        (@PaymentPolicyId, @TenantId, N'None', N'Percentage', 0.00, 100.00, 0.00, 0, 7, @UtcNow, @UtcNow);

    ----------------------------------------------------------------------
    -- 4. ApplicationUsers  (post-invite-redemption state: EmailConfirmed=1,
    --    IsActive=1 — same fields ResetPasswordAsync sets after a real
    --    invite/reset link is used)
    ----------------------------------------------------------------------
    INSERT INTO ApplicationUsers
        (Id, FirstName, LastName, IsPlatformAdmin, IsActive, CreatedAt, UpdatedAt, LastLoginAt, PhotoUrl,
         UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
         PasswordHash, SecurityStamp, ConcurrencyStamp,
         PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount)
    VALUES
        (@OwnerUserId, @OwnerFirstName, @OwnerLastName, 0, 1, @UtcNow, @UtcNow, NULL, NULL,
         @OwnerEmail, UPPER(@OwnerEmail), @OwnerEmail, UPPER(@OwnerEmail), 1,
         @OwnerPasswordHash, @OwnerSecurityStamp, @OwnerConcurrencyStamp,
         NULL, 0, 0, NULL, 1, 0),
        (@StaffUserId, @StaffFirstName, @StaffLastName, 0, 1, @UtcNow, @UtcNow, NULL, NULL,
         @StaffEmail, UPPER(@StaffEmail), @StaffEmail, UPPER(@StaffEmail), 1,
         @StaffPasswordHash, @StaffSecurityStamp, @StaffConcurrencyStamp,
         NULL, 0, 0, NULL, 1, 0);

    ----------------------------------------------------------------------
    -- 5. TenantMembers  (TenantMember.Invite -> Activate() -> AssignRole/
    --    AssignOwnerRole; Status='Active' since invite has been "redeemed";
    --    ApplicationUserId shadow FK left NULL — real code never sets it,
    --    only UserId is populated)
    ----------------------------------------------------------------------
    INSERT INTO TenantMembers
        (TenantMemberId, TenantId, branch_id, FirstName, LastName, Email, ContactNumber,
         Role, CustomJobTitle, UserId, PhotoUrl, Status, CreatedAt, UpdatedAt, ApplicationUserId)
    VALUES
        (@OwnerTenantMemberId, @TenantId, @BranchId, @OwnerFirstName, @OwnerLastName, @OwnerEmail, N'+639171234567',
         N'Owner', N'Primary Business Owner', @OwnerUserId, NULL, N'Active', @UtcNow, @UtcNow, NULL),
        (@StaffTenantMemberId, @TenantId, @BranchId, @StaffFirstName, @StaffLastName, @StaffEmail, N'+639171234568',
         N'Staff', N'Senior Barber', @StaffUserId, NULL, N'Active', @UtcNow, @UtcNow, NULL);

    -- Weekly schedule per member — matches the branch's real operating hours
    -- (Mon-Sat 09:00-19:00, Sun off) so both are actually bookable.
    INSERT INTO TenantMemberWeeklySchedules (DayOfWeek, TenantMemberId, StartTime, EndTime, IsOff)
    SELECT d.DayOfWeek, m.TenantMemberId, d.StartTime, d.EndTime, d.IsOff
    FROM (VALUES
        (N'Sunday',    CAST('00:00:00' AS TIME), CAST('00:00:00' AS TIME), CAST(1 AS BIT)),
        (N'Monday',    CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Tuesday',   CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Wednesday', CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Thursday',  CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Friday',    CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT)),
        (N'Saturday',  CAST('09:00:00' AS TIME), CAST('19:00:00' AS TIME), CAST(0 AS BIT))
    ) AS d(DayOfWeek, StartTime, EndTime, IsOff)
    CROSS JOIN (VALUES (@OwnerTenantMemberId), (@StaffTenantMemberId)) AS m(TenantMemberId);

    ----------------------------------------------------------------------
    -- 6. services  (Tenant.CreateService — barbershop catalog, PHP pricing,
    --    matches the currency code used throughout the codebase)
    ----------------------------------------------------------------------
    DECLARE @Services TABLE (ServiceId UNIQUEIDENTIFIER, Name NVARCHAR(255));

    INSERT INTO services
        (id, tenant_id, name, description, duration_minutes, buffer_before_minutes, buffer_after_minutes,
         price, currency_code, cancellation_policy_override, min_advance_booking_hours_override,
         is_active, created_at, updated_at)
    OUTPUT inserted.id, inserted.name INTO @Services(ServiceId, Name)
    SELECT NEWID(), @TenantId, s.Name, s.Description, s.DurationMinutes, s.BufferBefore, s.BufferAfter,
           s.Price, N'PHP', NULL, NULL, 1, @UtcNow, @UtcNow
    FROM (VALUES
        (N'Classic Haircut',        N'Traditional scissor-and-clipper haircut with styling.',        30, 0,  5, 250.00),
        (N'Beard Trim',             N'Beard shaping and edge cleanup.',                                20, 0,  5, 150.00),
        (N'Haircut + Beard Combo',  N'Full haircut paired with a beard trim.',                          45, 0,  5, 350.00),
        (N'Hot Towel Shave',        N'Classic straight-razor hot towel shave.',                         30, 5,  5, 200.00),
        (N'Kids Haircut (12 & under)', N'Haircut for young clients.',                                   25, 0,  5, 180.00),
        (N'Hair & Scalp Treatment', N'Deep-cleansing scalp treatment with scalp massage.',               40, 0, 10, 300.00)
    ) AS s(Name, Description, DurationMinutes, BufferBefore, BufferAfter, Price);

    ----------------------------------------------------------------------
    -- 7. MemberServices  (Tenant.AssignStaffToService — both team members
    --    can perform every service, so any service is bookable with either)
    ----------------------------------------------------------------------
    INSERT INTO MemberServices (TenantMemberId, ServiceId)
    SELECT m.TenantMemberId, sv.ServiceId
    FROM (VALUES (@OwnerTenantMemberId), (@StaffTenantMemberId)) AS m(TenantMemberId)
    CROSS JOIN @Services AS sv;

    COMMIT TRANSACTION;

    PRINT N'Demo barbershop tenant seeded successfully.';
    PRINT N'  Tenant slug:    demo-barbershop';
    PRINT N'  Business type:  BarberShop';
    PRINT N'  Owner login:    owner@demo.apexbooking.test / Demo@12345';
    PRINT N'  Staff login:    staff@demo.apexbooking.test / Demo@12345';
    PRINT N'  Services:       6 (all bookable with either team member)';
    PRINT N'  Hours:          Mon-Sat 09:00-19:00, Sun closed';

END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH
