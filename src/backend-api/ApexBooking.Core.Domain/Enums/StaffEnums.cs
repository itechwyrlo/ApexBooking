namespace ApexBooking.Core.Domain.Enums;

/// <summary>Staff employment lifecycle (05 §3).</summary>
public enum TenantMemberStatus
{
    Invited,
    Active,
    Deactivated
}

/// <summary>Whether a time-off request covers whole days or a window within days (05 §3).</summary>
public enum TimeOffType
{
    FullDay,
    PartialDay
}

/// <summary>Lifecycle of a staff time-off request (05 §3).</summary>
public enum TimeOffStatus
{
    Requested,
    Approved,
    Rejected
}
