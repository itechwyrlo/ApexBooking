using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class TenantMember : ITenantEntity
{
    public TenantMemberId TenantMemberId { get; protected set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public BranchId BranchId { get; private set; } = default!;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string ContactNumber { get; private set; } = string.Empty;
    public SystemRole Role { get; private set; } // Keeps its clean private set modifier
    public string? CustomJobTitle { get; private set; }
    public Guid? UserId { get; private set; }
    public string? PhotoUrl { get; private set; }
    public TenantMemberStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive => Status == TenantMemberStatus.Active;
    private readonly List<MemberService> _memberServices = [];
    public IReadOnlyCollection<MemberService> MemberServices => _memberServices.AsReadOnly();

    private readonly List<Booking> _appointments = [];
    public IReadOnlyCollection<Booking> Appointments => _appointments.AsReadOnly();
    private readonly List<DayScheduleEntry> _weeklySchedule = [];
    public IReadOnlyCollection<DayScheduleEntry> WeeklySchedule => _weeklySchedule.AsReadOnly();
    private readonly List<StaffBreak> _breaks = [];
    public IReadOnlyCollection<StaffBreak> Breaks => _breaks.AsReadOnly();

    private readonly List<StaffTimeOffRequest> _timeOffRequests = [];
    public IReadOnlyCollection<StaffTimeOffRequest> TimeOffRequests => _timeOffRequests.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected TenantMember() { }

    private TenantMember(TenantId tenantId, BranchId branchId, Guid applicationUserId, string firstName, string lastName, string email, string contactNumber, string? jobTitle)
    {
        TenantMemberId = new TenantMemberId(Guid.NewGuid());
        TenantId = tenantId;
        BranchId = branchId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        UserId = applicationUserId;
        ContactNumber = contactNumber;
        CustomJobTitle = jobTitle;
        Status = TenantMemberStatus.Invited;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            _weeklySchedule.Add(DayScheduleEntry.OffFor(day));
    }

    public static TenantMember Invite(TenantId tenantId, BranchId branchId, Guid applicationUserId, string firstName, string lastName, string email, string contactNumber = "", string? description = null)
    {
        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");

        if (branchId is null)
            throw new BusinessRuleBrokenException("A deployment branch is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new BusinessRuleBrokenException("Staff name is required.");

        if (string.IsNullOrWhiteSpace(email))
            throw new BusinessRuleBrokenException("Staff email is required.");

        return new TenantMember(tenantId, branchId, applicationUserId, firstName, lastName, email, contactNumber, description);
    }

    public void Activate()
    {
        if (Status == TenantMemberStatus.Active)
            throw new BusinessRuleBrokenException("Staff is already active.");

        if (Status == TenantMemberStatus.Deactivated)
            throw new BusinessRuleBrokenException("Deactivated staff cannot be reactivated here.");

        Status = TenantMemberStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == TenantMemberStatus.Deactivated)
            throw new BusinessRuleBrokenException("Staff is already inactive.");

        Status = TenantMemberStatus.Deactivated;
        UpdatedAt = DateTime.UtcNow;
    }

    // Email is intentionally excluded — it's copied from ApplicationUser at invite time and
    // nothing keeps the two in sync, so it isn't safe to edit through this path.
    public void UpdateProfile(string firstName, string lastName, string contactNumber, string? customJobTitle)
    {
        if (string.IsNullOrWhiteSpace(lastName))
            throw new BusinessRuleBrokenException("Staff name is required.");

        FirstName = firstName;
        LastName = lastName;
        ContactNumber = contactNumber;
        CustomJobTitle = customJobTitle;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePhoto(string? photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignRole(SystemRole assignedRole)
    {
        // Protect the boundary from illegal structural upgrades via general invitations
        if (assignedRole == SystemRole.Owner)
            throw new BusinessRuleBrokenException("Cannot assign Owner role through regular staff invitation workflows.");

        Role = assignedRole;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void AssignOwnerRole()
    {
        Role = SystemRole.Owner;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void TransferToBranch(BranchId branchId)
    {
        BranchId = branchId ?? throw new ArgumentNullException(nameof(branchId));
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Weekly schedule + breaks ───────────────────────────────────────────────

    public void SetDaySchedule(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isOff)
    {
        _weeklySchedule.RemoveAll(s => s.DayOfWeek == dayOfWeek);
        _weeklySchedule.Add(new DayScheduleEntry(dayOfWeek, startTime, endTime, isOff));
        UpdatedAt = DateTime.UtcNow;
    }

    public StaffBreak AddBreak(string name, TimeOnly startTime, TimeOnly endTime)
    {
        var entry = new StaffBreak(name, startTime, endTime);
        _breaks.Add(entry);
        UpdatedAt = DateTime.UtcNow;
        return entry;
    }

    public void RemoveBreak(StaffBreakId breakId)
    {
        var entry = _breaks.FirstOrDefault(b => b.Id == breakId)
            ?? throw new BusinessRuleBrokenException("Break not found.");

        _breaks.Remove(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Time off ───────────────────────────────────────────────────────────────

    public StaffTimeOffRequest RequestTimeOff(TimeOffType type, DateOnly startDate, DateOnly endDate, TimeOnly? startTime, TimeOnly? endTime, string? reason)
    {
        var request = new StaffTimeOffRequest(type, startDate, endDate, startTime, endTime, reason);
        _timeOffRequests.Add(request);
        UpdatedAt = DateTime.UtcNow;
        return request;
    }

    public void ApproveTimeOff(StaffTimeOffRequestId requestId)
    {
        var request = _timeOffRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new BusinessRuleBrokenException("Time-off request not found.");
        request.Approve();
        UpdatedAt = DateTime.UtcNow;
    }

    public void RejectTimeOff(StaffTimeOffRequestId requestId)
    {
        var request = _timeOffRequests.FirstOrDefault(r => r.Id == requestId)
            ?? throw new BusinessRuleBrokenException("Time-off request not found.");
        request.Reject();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Whether any approved time-off request covers this date — blocks both slot
    /// generation and real-time walk-in availability (and booking creation itself, as a
    /// server-side invariant re-check; see Tenant.PlaceBooking).</summary>
    public bool HasApprovedTimeOff(DateOnly date) => _timeOffRequests.Any(r => r.BlocksDate(date));

    // ── Real-time (walk-in) availability check ──────────────────────────────────

    /// <summary>
    /// Is this staff member free starting at this exact local moment for the given service window?
    /// Unlike whole-day slot generation, this checks a single point in time — used for walk-in
    /// admission. <paramref name="moment"/> must already be wall-clock local time, not UTC.
    /// <paramref name="existingBookings"/> may contain bookings for other staff/dates; only this
    /// member's own Scheduled bookings matching the moment's date are considered.
    /// </summary>
    public bool IsAvailableAt(DateTime moment, int durationMinutes, int bufferMinutes, IEnumerable<Booking> existingBookings)
    {
        var targetDate = DateOnly.FromDateTime(moment);
        if (HasApprovedTimeOff(targetDate))
            return false;

        var schedule = _weeklySchedule.FirstOrDefault(s => s.DayOfWeek == moment.DayOfWeek);
        if (schedule is null || schedule.IsOff)
            return false;

        var startTime = TimeOnly.FromDateTime(moment);
        var endTime = startTime.AddMinutes(durationMinutes);
        var totalBlockEndTime = startTime.AddMinutes(durationMinutes + bufferMinutes);

        if (startTime < schedule.StartTime || totalBlockEndTime > schedule.EndTime)
            return false;

        if (_breaks.Any(b => b.OverlapsWith(startTime, totalBlockEndTime)))
            return false;

        bool collidesWithBooking = existingBookings.Any(b =>
            b.StaffId == TenantMemberId
            && b.ScheduledDate == targetDate
            && b.Status == BookingStatus.Scheduled
            && b.ScheduledStartTime < totalBlockEndTime
            && b.ScheduledEndTime > startTime);

        return !collidesWithBooking;
    }
}
