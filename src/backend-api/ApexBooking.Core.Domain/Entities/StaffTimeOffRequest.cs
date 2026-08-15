using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.Entities;

/// <summary>
/// A staff time-off request with its own lifecycle (05 §3): Requested → Approved/Rejected.
/// A workflow with a decision attached, not just descriptive data. Accessed only through the
/// Staff aggregate root. Approved time off blocks availability in the scheduling engine (06 §1).
/// </summary>
public class StaffTimeOffRequest
{
    public StaffTimeOffRequestId Id { get; private set; } = default!;
    public TimeOffType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }

    /// <summary>Set only for <see cref="TimeOffType.PartialDay"/>.</summary>
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }

    public TimeOffStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }

    protected StaffTimeOffRequest() { }

    internal StaffTimeOffRequest(
        TimeOffType type,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? reason)
    {
        if (endDate < startDate)
            throw new BusinessRuleBrokenException("Time-off end date cannot be before the start date.");

        if (type == TimeOffType.PartialDay)
        {
            if (startTime is null || endTime is null)
                throw new BusinessRuleBrokenException("Partial-day time off requires a start and end time.");

            if (startTime >= endTime)
                throw new BusinessRuleBrokenException("Time-off start time must be before end time.");
        }

        Id = new StaffTimeOffRequestId(Guid.NewGuid());
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        StartTime = type == TimeOffType.PartialDay ? startTime : null;
        EndTime = type == TimeOffType.PartialDay ? endTime : null;
        Status = TimeOffStatus.Requested;
        Reason = reason;
        RequestedAt = DateTime.UtcNow;
    }

    internal void Approve()
    {
        if (Status != TimeOffStatus.Requested)
            throw new BusinessRuleBrokenException("Only requested time off can be approved.");

        Status = TimeOffStatus.Approved;
        DecidedAt = DateTime.UtcNow;
    }

    internal void Reject()
    {
        if (Status != TimeOffStatus.Requested)
            throw new BusinessRuleBrokenException("Only requested time off can be rejected.");

        Status = TimeOffStatus.Rejected;
        DecidedAt = DateTime.UtcNow;
    }

    /// <summary>Whether this approved time off blocks the given date (used by the slot calculator).</summary>
    public bool BlocksDate(DateOnly date) =>
        Status == TimeOffStatus.Approved && date >= StartDate && date <= EndDate;
}
