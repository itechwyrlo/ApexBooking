using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetBranch
{
    public record OperatingHoursEntryDto(
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        TimeOnly EndTime,
        bool IsOff
    );

    public record BranchDetailDto(
        Guid BranchId,
        string BranchName,
        string Street,
        string? Barangay,
        string City,
        string Province,
        string ZipCode,
        string TimeZoneId,
        bool IsActive,
        IReadOnlyCollection<OperatingHoursEntryDto> OperatingHours
    );

    public record GetBranchQuery(Guid BranchId) : IQuery<BranchDetailDto>;
}
