using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

/// <summary>
/// Child of ResourceAvailabilitySchedule. Grandchild of Resource.
/// Defines a recurring break window within a working day.
/// Cannot exist without its parent ResourceAvailabilitySchedule.
/// TR-9.1 Step 5, UC-3.1.3
/// </summary>
public class ResourceBreakPeriod : ITenantEntity
{
    public ResourceBreakPeriodId ResourceBreakPeriodId { get; private set; } = default!;
    public ResourceAvailabilityScheduleId ResourceAvailabilityScheduleId { get; private set; } = default!;
    public ResourceId ResourceId { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public TimeOnly BreakStartTime { get; private set; }
    public TimeOnly BreakEndTime { get; private set; }

    /// <summary>
    /// Optional label e.g. "Lunch Break". For display purposes only.
    /// </summary>
    public string? Label { get; private set; }

    public DateTime CreatedAt { get; private set; }

    protected ResourceBreakPeriod() { }

    private ResourceBreakPeriod(
        ResourceAvailabilityScheduleId scheduleId,
        ResourceId resourceId,
        TenantId tenantId,
        TimeOnly breakStartTime,
        TimeOnly breakEndTime,
        string? label)
    {
        ResourceBreakPeriodId = new ResourceBreakPeriodId(Guid.NewGuid());
        ResourceAvailabilityScheduleId = scheduleId;
        ResourceId = resourceId;
        TenantId = tenantId;
        BreakStartTime = breakStartTime;
        BreakEndTime = breakEndTime;
        Label = label;
        CreatedAt = DateTime.UtcNow;
    }

    internal static ResourceBreakPeriod Create(
        ResourceAvailabilityScheduleId scheduleId,
        ResourceId resourceId,
        TenantId tenantId,
        TimeOnly breakStartTime,
        TimeOnly breakEndTime,
        string? label = null)
    {
        if (breakStartTime >= breakEndTime)
            throw new BusinessRuleBrokenException("Break start time must be before break end time.");

        return new ResourceBreakPeriod(scheduleId, resourceId, tenantId, breakStartTime, breakEndTime, label);
    }

    /// <summary>
    /// Returns true if this break overlaps with the given time window.
    /// Used by the slot calculator to subtract blocked windows.
    /// </summary>
    public bool OverlapsWith(TimeOnly windowStart, TimeOnly windowEnd)
        => BreakStartTime < windowEnd && BreakEndTime > windowStart;
}