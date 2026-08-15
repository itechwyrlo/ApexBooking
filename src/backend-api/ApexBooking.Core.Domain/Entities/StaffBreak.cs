using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.Entities;

public class StaffBreak
{
    public StaffBreakId Id { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    protected StaffBreak() { }

    internal StaffBreak(string name, TimeOnly startTime, TimeOnly endTime)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleBrokenException("Break name is required.");

        if (startTime >= endTime)
            throw new BusinessRuleBrokenException("Break start time must be before end time.");

        Id = new StaffBreakId(Guid.NewGuid());
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
    }
    public bool OverlapsWith(TimeOnly windowStart, TimeOnly windowEnd)
        => StartTime < windowEnd && EndTime > windowStart;
}
