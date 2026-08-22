using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Policies;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities
{
    public sealed class Tenant : IAggregateRoot, IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        public TenantId TenantId { get; private set; } = default!;
        public string? Slug { get; private set; }

        public OwnerContact OwnerContact { get; private set; } = default!;

        public SubscriptionPlanType Plan { get; private set; }

        public bool IsActive { get; private set; }
        public bool SetupRequired { get; private set; }
        public bool SetupCompleted { get; private set; }

        public TrialPeriod? Trial { get; private set; }

        public DateTime? TrialExpiredAt { get; private set; }

        public DateTime? TrialReminderSentAt { get; private set; }
        public BusinessProfile BusinessProfile { get; private set; } = default!;
        public BookingPolicy? BookingPolicy { get; private set; }
        public PaymentPolicy? PaymentPolicy { get; private set; }
        public TenantPaymentCredential? PaymentCredential { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public DateTime? DeactivatedAt { get; private set; }

        private readonly List<TenantMember> _tenantMembers = [];
        public IReadOnlyCollection<TenantMember> Members => _tenantMembers.AsReadOnly();
        private readonly List<Service> _services = [];
        public IReadOnlyCollection<Service> Services => _services.AsReadOnly();
        private readonly List<Branch> _branches = [];
        public IReadOnlyCollection<Branch> Branches => _branches.AsReadOnly();
        private readonly List<Booking> _bookings = [];
        public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

        public IReadOnlyCollection<IDomainEvent> DomainEvents
            => _domainEvents.AsReadOnly();

        public void ClearDomainEvents() => _domainEvents.Clear();

        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);

        private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        protected Tenant()
        {
        }

        private Tenant(
            string slug,
            OwnerContact ownerContact,
            SubscriptionPlanType requestedPlan,
            string businessName,
            BusinessType businessType,
            DateTime utcNow)
        {
            TenantId = new TenantId(Guid.NewGuid());
            Slug = slug.ToLowerInvariant().Trim();
            OwnerContact = ownerContact;
            Plan = requestedPlan;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
            BusinessProfile = BusinessProfile.CreateDefault(businessName, businessType);
            BookingPolicy = BookingPolicy.CreateDefault(TenantId);
            PaymentPolicy = PaymentPolicy.CreateDefault(TenantId);
            IsActive = true;
            SetupRequired = true;
            SetupCompleted = false;

            AddDomainEvent(new TenantCreatedDomainEvent(
                TenantId,
                Slug,
                businessName,
                ownerContact.FirstName,
                ownerContact.LastName,
                ownerContact.Email,
                utcNow));
        }

        public static Tenant Create(
           string slug,
           OwnerContact ownerContact,
           SubscriptionPlanType requestedPlan,
           string businessName,
           BusinessType businessType,
           ISlugValidationPolicy slugPolicy,
           DateTime utcNow)
        {
            if (string.IsNullOrWhiteSpace(businessName))
                throw new BusinessRuleBrokenException("Business name cannot be empty.");

            if (!slugPolicy.IsValid(slug))
                throw new BusinessRuleBrokenException($"The slug '{slug}' format is invalid or contains forbidden characters.");

            return new Tenant(slug, ownerContact, requestedPlan, businessName, businessType, utcNow);
        }

        public BranchId PrimaryBranchId => _branches.OrderBy(b => b.CreatedAt).First().BranchId;

        // ── Branch Orchestration Gatekeepers ───────────────────────────────────

        public Branch AddBranch(string branchName, string timeZoneId, Address address)
        {
            if (_branches.Any(b => b.BranchName.Equals(branchName.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new BusinessRuleBrokenException("A branch with this name already exists in your organization.");

            var branch = Branch.Create(TenantId, branchName, timeZoneId, address, DateTime.UtcNow);
            _branches.Add(branch);
            UpdatedAt = DateTime.UtcNow;
            return branch;
        }

        public void UpdateBranchProfile(BranchId branchId, string branchName, string timeZoneId, Address address)
        {
            var branch = RequireBranch(branchId);

            if (_branches.Any(b => b.BranchId != branchId &&
                                   b.BranchName.Equals(branchName.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new BusinessRuleBrokenException("Another branch already claims this name.");

            branch.UpdateProfile(branchName, timeZoneId, address);
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetBranchOperatingHours(BranchId branchId, DayOfWeek day, TimeOnly start, TimeOnly end, bool isOff)
        {
            RequireBranch(branchId).SetOperatingHours(day, start, end, isOff);
            UpdatedAt = DateTime.UtcNow;
        }

        private Branch RequireBranch(BranchId branchId) =>
            _branches.FirstOrDefault(b => b.BranchId == branchId)
                ?? throw new BusinessRuleBrokenException("The target branch does not exist inside this organization.");

        // ── Staff Provisioning & Role Assignments ──────────────────────────────

        public TenantMember InviteMember(
            Guid applicationUserId,
            BranchId branchId,
            string firstName,
            string lastName,
            string email,
            SystemRole assignedRole,
            string contactNumber = "",
            string? description = null)
        {
            RequireBranch(branchId);

            if (_tenantMembers.Any(m => m.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                throw new BusinessRuleBrokenException($"A team member with the email '{email}' already exists in this business.");


            var newMember = TenantMember.Invite(
                tenantId: this.TenantId,
                branchId: branchId,
                applicationUserId: applicationUserId,
                firstName: firstName,
                lastName: lastName,
                email: email,
                contactNumber: contactNumber,
                description: description
            );

            newMember.Activate();

            // 🐛 Fixes the provisioning crash: Owner routes through a dedicated aggregate-internal
            // assignment instead of AssignRole, which rejects Owner by design (see TenantMember.AssignRole).
            if (assignedRole == SystemRole.Owner)
                newMember.AssignOwnerRole();
            else
                newMember.AssignRole(assignedRole);

            _tenantMembers.Add(newMember);
            this.UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new TeamMemberInvitedDomainEvent(
                TenantId: this.TenantId,
                Slug: this.Slug!,
                TenantMemberId: newMember.TenantMemberId.Value, // Assuming strongly-typed ID unwrap
                ApplicationUserId: applicationUserId,
                Email: email,
                FullName: $"{firstName} {lastName}".Trim(),
                AssignedRole: assignedRole,
                InvitedAt: DateTime.UtcNow
            ));

            return newMember;
        }

        public void UpdateMemberProfile(
            TenantMemberId tenantMemberId,
            string firstName,
            string lastName,
            string contactNumber,
            string? customJobTitle,
            SystemRole role)
        {
            var member = RequireMember(tenantMemberId);

            // Neither direction is allowed through this path: promoting someone to Owner, or
            // demoting the current Owner away from it, are both ownership transfers.
            if (role == SystemRole.Owner || member.Role == SystemRole.Owner)
            {
                if (role != member.Role)
                    throw new BusinessRuleBrokenException("Ownership cannot be transferred through team member editing.");
            }
            else
            {
                member.AssignRole(role);
            }

            member.UpdateProfile(firstName, lastName, contactNumber, customJobTitle);
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void DeactivateMember(TenantMemberId tenantMemberId)
        {
            var member = RequireMember(tenantMemberId);

            if (member.Role == SystemRole.Owner)
                throw new BusinessRuleBrokenException("The business owner cannot be deactivated.");

            member.Deactivate();
            this.UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new StaffDeactivatedDomainEvent(
                TenantId: this.TenantId,
                TenantMemberId: member.TenantMemberId.Value,
                FullName: $"{member.FirstName} {member.LastName}".Trim(),
                DeactivatedAt: this.UpdatedAt
            ));
        }

        // Only safe to call once the caller has confirmed (e.g. via a "has historical records"
        // check) that no Booking references this member — Booking.StaffId is DeleteBehavior
        // .Restrict, so a member with any booking history would fail at the database level anyway.
        public void RemoveMember(TenantMemberId tenantMemberId)
        {
            var member = RequireMember(tenantMemberId);

            if (member.Role == SystemRole.Owner)
                throw new BusinessRuleBrokenException("The business owner cannot be removed.");

            _tenantMembers.Remove(member);
            this.UpdatedAt = DateTime.UtcNow;
        }

        private TenantMember RequireMember(TenantMemberId tenantMemberId) =>
            _tenantMembers.FirstOrDefault(m => m.TenantMemberId == tenantMemberId)
                ?? throw new BusinessRuleBrokenException("Target team member record was not found within this business workspace.");

        public void MarkSetupCompleted()
        {
            SetupCompleted = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public Service CreateService(
        string name,
        int durationMinutes,
        decimal price,
        string currencyCode,
        string? description = null,
        int bufferBeforeMinutes = 0,
        int bufferAfterMinutes = 0,
        int? minAdvanceBookingHoursOverride = null)
        {
            if (_services.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new BusinessRuleBrokenException("A service with this name already exists in your business catalog.");

            var newService = Service.Create(
                this.TenantId,
                name,
                durationMinutes,
                price,
                currencyCode,
                description,
                bufferBeforeMinutes,
                bufferAfterMinutes,
                minAdvanceBookingHoursOverride
            );

            _services.Add(newService);
            this.UpdatedAt = DateTime.UtcNow;

            return newService;
        }

        public void UpdateService(
        Guid serviceId,
        string name,
        string? description,
        int durationMinutes,
        decimal price,
        string currencyCode,
        int bufferBeforeMinutes,
        int bufferAfterMinutes,
        int? minAdvanceBookingHoursOverride = null)
        {
            var service = _services.FirstOrDefault(s => s.ServiceId.Value == serviceId);
            if (service == null)
                throw new BusinessRuleBrokenException("The target service item was not found inside this business catalog.");

            if (!service.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var nameExists = _services.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (nameExists)
                    throw new BusinessRuleBrokenException("Cannot update item. Another catalog service already claims this name.");
            }

            service.Update(
                name: name,
                description: description,
                durationMinutes: durationMinutes,
                price: price,
                currencyCode: currencyCode,
                bufferBeforeMinutes: bufferBeforeMinutes,
                bufferAfterMinutes: bufferAfterMinutes,
                minAdvanceBookingHoursOverride: minAdvanceBookingHoursOverride
            );

            this.UpdatedAt = DateTime.UtcNow;
        }

        public void AssignStaffToService(Guid serviceId, Guid tenantMemberId)
        {
            var service = _services.FirstOrDefault(s => s.ServiceId.Value == serviceId)
                ?? throw new BusinessRuleBrokenException("The target service item was not found inside this business catalog.");

            var member = _tenantMembers.FirstOrDefault(m => m.TenantMemberId.Value == tenantMemberId)
                ?? throw new BusinessRuleBrokenException("The target team member was not found inside this business workspace.");

            var provider = MemberService.Create(member.TenantMemberId, service.ServiceId);
            service.AssignStaffMember(provider);

            this.UpdatedAt = DateTime.UtcNow;
        }

        public void UnassignStaffFromService(Guid serviceId, Guid tenantMemberId)
        {
            var service = _services.FirstOrDefault(s => s.ServiceId.Value == serviceId)
                ?? throw new BusinessRuleBrokenException("The target service item was not found inside this business catalog.");

            service.RemoveStaffMember(new TenantMemberId(tenantMemberId));

            this.UpdatedAt = DateTime.UtcNow;
        }

        // ── Minimalist Operational Booking Domain Entry Points ────────────────

        public Booking ScheduleBooking(
            BranchId branchId,
            CustomerId customerId,
            TenantMemberId staffId,
            ServiceId serviceId,
            DateOnly date,
            TimeOnly startTime,
            string? customerNotes,
            bool admitImmediately)
            // Admin dashboard bookings skip the 'PendingPayment' step entirely — always paid in
            // person on the spot. PlaceBooking snapshots the service price as AmountDue and leaves
            // PaymentConfirmedVia null (pay-in-visit, pending) until CompleteService() collects it.
            // admitImmediately is true only for a staff member's immediate "available now" slot —
            // the caller (ScheduleBookingHandler) never sets it for a later-today slot.
            => PlaceBooking(branchId, customerId, staffId, serviceId, date, startTime, customerNotes,
                requiresUpfrontPayment: false, admitImmediately: admitImmediately);

        // Public wizard entry point: payment requirement is driven by the tenant's PaymentPolicy at the call site.
        public Booking PlaceCustomerBooking(
            BranchId branchId,
            CustomerId customerId,
            TenantMemberId staffId,
            ServiceId serviceId,
            DateOnly date,
            TimeOnly startTime,
            string? customerNotes,
            bool requiresUpfrontPayment,
            decimal amountDue = 0m)
            => PlaceBooking(branchId, customerId, staffId, serviceId, date, startTime, customerNotes, requiresUpfrontPayment, amountDue);

        private Booking PlaceBooking(
            BranchId branchId,
            CustomerId customerId,
            TenantMemberId staffId,
            ServiceId serviceId,
            DateOnly date,
            TimeOnly startTime,
            string? customerNotes,
            bool requiresUpfrontPayment,
            decimal amountDue = 0m,
            bool admitImmediately = false)
        {
            RequireBranch(branchId);

            // 1. Locate the service snapshot context in the internal aggregate catalog array
            var service = _services.FirstOrDefault(s => s.ServiceId == serviceId);
            if (service == null || !service.IsActive)
                throw new BusinessRuleBrokenException("Cannot book appointment. The target service catalog item is missing or inactive.");

            // 2. Locate the staff profile assignment inside this specific tenant
            var staff = _tenantMembers.FirstOrDefault(m => m.TenantMemberId == staffId);
            if (staff == null || !staff.IsActive)
                throw new BusinessRuleBrokenException("The requested staff member record does not exist or is currently inactive.");

            // 3. Enforce the staff-location invariant: bookings can't be placed against a branch the staff isn't deployed to
            if (staff.BranchId != branchId)
                throw new BusinessRuleBrokenException("The selected staff member is not deployed to the chosen branch.");

            // 🌟 INVARIANT GUARD: re-verify approved time off server-side — don't just trust that
            // slot listing / walk-in availability filtering was honored by the caller.
            if (staff.HasApprovedTimeOff(date))
                throw new BusinessRuleBrokenException("The selected staff member is on approved time off during this date.");

            // 🌟 INVARIANT GUARD: re-verify this staff member has no other appointment covering this
            // window, server-side, at commit time — availability may have changed since it was
            // calculated (another booking could have landed in between). Same overlap shape as
            // TenantMember.IsAvailableAt / SlotGenerator, enforced here so BOTH booking entry points
            // (public wizard and admin/walk-in) share one final, authoritative check.
            var newBlockEnd = startTime.AddMinutes(service.DurationMinutes + service.BufferAfterMinutes);
            bool collidesWithExistingBooking = _bookings.Any(b =>
                b.StaffId == staffId
                && b.ScheduledDate == date
                // PendingPayment must block the slot too — otherwise two customers can both reach
                // checkout for the same staff/time before either one pays (see
                // 2026-08-18-payment-booking-security-hardening-design.md, Module 2). The
                // InitiateBookingHandler-held sp_getapplock (IUnitOfWork.AcquireBookingLockAsync)
                // is what makes this check race-free against a second concurrent request; this
                // predicate is what makes it correct once only one request is in here at a time.
                && (b.Status == BookingStatus.Scheduled || b.Status == BookingStatus.PendingPayment)
                && b.ScheduledStartTime < newBlockEnd
                && b.ScheduledEndTime > startTime);
            if (collidesWithExistingBooking)
                throw new BusinessRuleBrokenException("This time slot is no longer available for the selected staff member. Please choose another time.");

            // 4. Generate a human-readable booking tracking reference string (e.g. APX-X7B2-9M)
            string bookingReference = $"APX-{Guid.NewGuid().ToString()[..4]}-{Guid.NewGuid().ToString()[..2]}".ToUpperInvariant();

            // Pay-in-visit bookings (no upfront payment required) always owe the full service
            // price at the visit — snapshot it here regardless of what the caller passed, so both
            // walk-ins (which never pass one) and the public wizard's None-policy path get it for
            // free from one authoritative place.
            decimal finalAmountDue = requiresUpfrontPayment ? amountDue : service.Price;

            // 5. Instantiate the child entity passing snapshotted menu variables natively
            var booking = Booking.Create(
                tenantId: this.TenantId,
                branchId: branchId,
                customerId: customerId,
                staffId: staffId,
                serviceId: serviceId,
                bookingReference: bookingReference,
                scheduledDate: date,
                scheduledStartTime: startTime,
                durationMinutes: service.DurationMinutes, // Snapshotted history tracking protection
                bufferAfterMinutes: service.BufferAfterMinutes, // Snapshotted history tracking protection
                customerNotes: customerNotes,
                requiresUpfrontPayment: requiresUpfrontPayment,
                currencyCode: service.CurrencyCode,
                amountDue: finalAmountDue,
                servicePriceAtBooking: service.Price
            );

            // Walk-in picked the immediate "available now" slot — the customer is already here,
            // there's no separate arrival to wait for. A later-today slot on an otherwise-busy
            // staff member stays un-admitted, same as any other scheduled appointment.
            if (admitImmediately)
                booking.RecordArrival();

            _bookings.Add(booking);
            this.UpdatedAt = DateTime.UtcNow;

            return booking;
        }

        // ── Secure QR Boarding Pass Scan Entry Point ────────────────────────────

        public (Booking Booking, bool WasFirstAdmission) RecordBookingArrival(BookingId bookingId, BranchId scannerBranchId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            // 🌟 Cross-branch fraud guard: the scanning branch must match the booking's own branch
            if (booking.BranchId != scannerBranchId)
                throw new BusinessRuleBrokenException("This boarding pass belongs to a different branch and cannot be scanned here.");

            // Clears the way for check-in without asserting payment was captured — that happens
            // for real at checkout now (see Booking.ClearPendingPaymentOnArrival).
            if (booking.Status == BookingStatus.PendingPayment)
                booking.ClearPendingPaymentOnArrival();

            var wasFirstAdmission = booking.RecordArrival();
            this.UpdatedAt = DateTime.UtcNow;

            return (booking, wasFirstAdmission);
        }

        public void CompleteBooking(Guid bookingId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            // Delegates state modification down to child entity safely
            booking.CompleteService();
            this.UpdatedAt = DateTime.UtcNow;
        }

        // The new checkout-scan entry point — distinct from CompleteBooking (the existing manual
        // Complete button/command, which stays untouched and unguarded as an Owner/Admin
        // fallback for e.g. a lost QR code). This one requires the booking to have actually been
        // checked in first, and gives state-specific errors for the other terminal statuses.
        public Booking CheckOutBooking(Guid bookingId, BranchId scannerBranchId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            if (booking.BranchId != scannerBranchId)
                throw new BusinessRuleBrokenException("This boarding pass belongs to a different branch and cannot be scanned here.");

            if (booking.CheckedInAt is null)
                throw new BusinessRuleBrokenException("This booking hasn't been checked in yet.");

            if (booking.Status == BookingStatus.Completed)
                throw new BusinessRuleBrokenException("This booking has already been completed.");

            if (booking.Status == BookingStatus.Cancelled)
                throw new BusinessRuleBrokenException("This booking was cancelled.");

            if (booking.Status == BookingStatus.NoShow)
                throw new BusinessRuleBrokenException("This booking was marked as a no-show.");

            booking.CompleteService();
            this.UpdatedAt = DateTime.UtcNow;

            return booking;
        }

        public void SetBookingStaffNotes(Guid bookingId, string notes)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            booking.SetStaffNotes(notes);
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void ReassignBooking(Guid bookingId, Guid newStaffId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            var newStaffMember = _tenantMembers.FirstOrDefault(m => m.TenantMemberId.Value == newStaffId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("The selected staff member is not available.");

            if (newStaffMember.BranchId != booking.BranchId)
                throw new BusinessRuleBrokenException("The selected staff member is not deployed to this booking's branch.");

            var service = _services.FirstOrDefault(s => s.ServiceId == booking.ServiceId)
                ?? throw new BusinessRuleBrokenException("This booking's service could not be found.");

            if (!service.ServiceProviders.Any(sp => sp.TenantMemberId == newStaffMember.TenantMemberId))
                throw new BusinessRuleBrokenException("The selected staff member is not assigned to this booking's service.");

            booking.Reassign(newStaffMember.TenantMemberId);
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void RecordBookingPayment(Guid bookingId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            booking.RecordPayInVisitPayment();
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void FlagBookingAsNoShow(Guid bookingId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            booking.MarkAsNoShow();
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void CancelBooking(Guid bookingId, Guid executionUserId, string reason, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            booking.Cancel(executionUserId, reason, BookingPolicy, PaymentPolicy, ewalletProvider, ewalletNumber, ewalletName);
            this.UpdatedAt = DateTime.UtcNow;
        }

        // Customer-initiated cancellation via the emailed cancel link. Unlike the staff path
        // above, this enforces the tenant's own notice window before it's allowed at all — a
        // staff member cancelling on the business's behalf isn't bound by the same cutoff.
        public void CancelBookingByCustomer(Guid bookingId, string reason, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId.Value == bookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            var scheduledAt = booking.ScheduledDate.ToDateTime(booking.ScheduledStartTime);
            var cutoffHours = BookingPolicy?.CancellationCutoffHours ?? 0;
            if (DateTime.UtcNow.AddHours(cutoffHours) > scheduledAt)
                throw new BusinessRuleBrokenException(
                    $"This booking can no longer be cancelled online — it's within {cutoffHours} hour(s) of the appointment. Please contact the business directly.");

            booking.CancelByCustomer(reason, BookingPolicy, PaymentPolicy, ewalletProvider, ewalletNumber, ewalletName);
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void ConfigurePaymentGateway(string secretKey, string publicKey)
        {
            if (PaymentCredential == null)
            {
                PaymentCredential = new TenantPaymentCredential(this.TenantId, secretKey, publicKey);
            }
            else
            {
                PaymentCredential.UpdateCredentials(secretKey, publicKey);
            }
            this.UpdatedAt = DateTime.UtcNow;
        }

        public void SetPaymentGatewayWebhookSecret(string webhookSecret)
        {
            if (PaymentCredential == null)
                throw new BusinessRuleBrokenException("Configure your PayMongo API keys before adding a webhook secret.");

            PaymentCredential.SetWebhookSecret(webhookSecret);
            this.UpdatedAt = DateTime.UtcNow;
        }

        // ── Trial Lifecycle ─────────────────────────────────────────────────

        // Does NOT touch IsActive — see TrialPeriod's doc comment (05 §1 / ADR-058): an
        // expired-trial tenant can still log in, the restriction is enforced where bookings
        // are created. Idempotent, so a job re-running over the same tenant is harmless.
        public void ExpireTrial()
        {
            if (TrialExpiredAt is not null)
                return;

            TrialExpiredAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new TrialExpiredDomainEvent(TenantId));
        }

        public void MarkTrialReminderSent()
        {
            if (TrialReminderSentAt is not null)
                return;

            TrialReminderSentAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new TrialReminderSentDomainEvent(TenantId));
        }
    }
}

