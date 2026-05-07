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
/// Child of Resource.
/// Overrides the base weekly schedule for a specific date.
/// TR-9.1 Step 4, TR-7.5, UC-3.1.4
/// </summary>
public class ResourceAvailabilityException : ITenantEntity
{
    public ResourceAvailabilityExceptionId ResourceAvailabilityExceptionId { get; private set; } = default!;
    public ResourceId ResourceId { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;

    /// <summary>
    /// The specific calendar date this exception applies to.
    /// </summary>
    public DateOnly ExceptionDate { get; private set; }

    public ExceptionType ExceptionType { get; private set; }

    /// <summary>
    /// Null when ExceptionType is UnavailableAllDay.
    /// Required for UnavailableHours and AvailableExtraHours.
    /// </summary>
    public TimeOnly? StartTime { get; private set; }

    /// <summary>
    /// Null when ExceptionType is UnavailableAllDay.
    /// Required for UnavailableHours and AvailableExtraHours.
    /// </summary>
    public TimeOnly? EndTime { get; private set; }

    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected ResourceAvailabilityException() { }

    private ResourceAvailabilityException(
        ResourceId resourceId,
        TenantId tenantId,
        DateOnly exceptionDate,
        ExceptionType exceptionType,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? note)
    {
        ResourceAvailabilityExceptionId = new ResourceAvailabilityExceptionId(Guid.NewGuid());
        ResourceId = resourceId;
        TenantId = tenantId;
        ExceptionDate = exceptionDate;
        ExceptionType = exceptionType;
        StartTime = startTime;
        EndTime = endTime;
        Note = note;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    internal static ResourceAvailabilityException Create(
        ResourceId resourceId,
        TenantId tenantId,
        DateOnly exceptionDate,
        ExceptionType exceptionType,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? note)
    {
        if (exceptionType != ExceptionType.UnavailableAllDay)
        {
            if (startTime is null)
                throw new BusinessRuleBrokenException("Start time is required for this exception type.");

            if (endTime is null)
                throw new BusinessRuleBrokenException("End time is required for this exception type.");

            if (startTime >= endTime)
                throw new BusinessRuleBrokenException("Start time must be before end time.");
        }

        return new ResourceAvailabilityException(resourceId, tenantId, exceptionDate, exceptionType, startTime, endTime, note);
    }

    public void UpdateNote(string? note)
    {
        Note = note;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the time window this exception applies to.
    /// Returns null for UnavailableAllDay since the entire day is blocked.
    /// Used by the slot calculator in TR-9.1 Step 4 and Step 8.
    /// </summary>
    public (TimeOnly Start, TimeOnly End)? GetTimeWindow()
    {
        if (ExceptionType == ExceptionType.UnavailableAllDay)
            return null;

        return (StartTime!.Value, EndTime!.Value);
    }
}

public enum ExceptionType
{
    UnavailableAllDay,
    UnavailableHours,
    AvailableExtraHours
}