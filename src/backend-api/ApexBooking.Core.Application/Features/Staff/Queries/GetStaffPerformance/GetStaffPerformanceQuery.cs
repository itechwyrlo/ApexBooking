using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staff.Queries.GetStaffPerformance
{
    public record GetStaffPerformanceQuery(DateOnly FromDate, DateOnly ToDate) : IQuery<IReadOnlyCollection<StaffPerformanceEntryDto>>;
}
