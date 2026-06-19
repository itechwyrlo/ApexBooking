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
/// Child of Resource. One record per day of week.
/// Defines the recurring weekly working hours for the resource.
/// Owned entirely by Resource — never created or queried independently.
/// TR-7.4, UC-3.1.3
/// </summary>
public class StaffAvailabilitySchedule : ITenantEntity
{
    public StaffAvailabilityScheduleId StaffAvailabilityScheduleId { get; private set; } = default!;
    public StaffId StaffId { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;

    /// <summary>
    /// 0 = Sunday through 6 = Saturday. Maps to System.DayOfWeek.
    /// </summary>
    public DayOfWeek DayOfWeek { get; private set; }

    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Required when IsAvailable is true. Null when IsAvailable is false.
    /// </summary>
    public TimeOnly? StartTime { get; private set; }

    /// <summary>
    /// Required when IsAvailable is true. Null when IsAvailable is false.
    /// </summary>
    public TimeOnly? EndTime { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Children: break periods within this day
    private readonly List<StaffBreakPeriod> _breakPeriods = new();
    public IReadOnlyCollection<StaffBreakPeriod> BreakPeriods => _breakPeriods.AsReadOnly();

    protected StaffAvailabilitySchedule() { }

    private StaffAvailabilitySchedule(
        StaffId staffId,
        TenantId tenantId,
        DayOfWeek dayOfWeek,
        bool isAvailable,
        TimeOnly? startTime,
        TimeOnly? endTime)
    {
        StaffAvailabilityScheduleId = new StaffAvailabilityScheduleId(Guid.NewGuid());
        StaffId = staffId;
        TenantId = tenantId;
        DayOfWeek = dayOfWeek;
        IsAvailable = isAvailable;
        StartTime = startTime;
        EndTime = endTime;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static StaffAvailabilitySchedule Create(
        StaffId staffId,
        TenantId tenantId,
        DayOfWeek dayOfWeek,
        bool isAvailable,
        TimeOnly? startTime,
        TimeOnly? endTime)
    {
        if (isAvailable)
        {
            if (startTime is null)
                throw new BusinessRuleBrokenException($"Start time is required for available day {dayOfWeek}.");

            if (endTime is null)
                throw new BusinessRuleBrokenException($"End time is required for available day {dayOfWeek}.");

            if (startTime >= endTime)
                throw new BusinessRuleBrokenException($"Start time must be before end time on {dayOfWeek}.");
        }

        return new StaffAvailabilitySchedule(staffId, tenantId, dayOfWeek, isAvailable, startTime, endTime);
    }

    // ── Break Period Management ───────────────────────────────────────────────

    /// <summary>
    /// Adds a recurring break within this day's working hours.
    /// Break must fall within the working window and must not overlap existing breaks.
    /// </summary>
    public void AddBreakPeriod(TimeOnly breakStart, TimeOnly breakEnd, string? label = null)
    {
        if (!IsAvailable)
            throw new BusinessRuleBrokenException("Cannot add break periods to an unavailable day.");

        if (breakStart >= breakEnd)
            throw new BusinessRuleBrokenException("Break start must be before break end.");

        if (breakStart < StartTime || breakEnd > EndTime)
            throw new BusinessRuleBrokenException("Break period must fall within working hours.");

        bool overlaps = _breakPeriods.Any(b => breakStart < b.BreakEndTime && breakEnd > b.BreakStartTime);
        if (overlaps)
            throw new BusinessRuleBrokenException("Break period overlaps with an existing break.");

        var breakPeriod = StaffBreakPeriod.Create(StaffAvailabilityScheduleId, StaffId, TenantId, breakStart, breakEnd, label);
        _breakPeriods.Add(breakPeriod);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveBreakPeriod(StaffBreakPeriodId breakPeriodId)
    {
        var breakPeriod = _breakPeriods.FirstOrDefault(b => b.StaffBreakPeriodId == breakPeriodId);
        if (breakPeriod is null)
            throw new BusinessRuleBrokenException("Break period not found.");

        _breakPeriods.Remove(breakPeriod);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearBreakPeriods()
    {
        _breakPeriods.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}