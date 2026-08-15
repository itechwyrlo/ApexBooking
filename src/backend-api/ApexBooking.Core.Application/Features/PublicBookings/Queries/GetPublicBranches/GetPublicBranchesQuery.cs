using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicBranches
{
    public record BranchSummary(
        Guid BranchId,
        string BranchName,
        string Street,
        string? Barangay,
        string City,
        string Province,
        string ZipCode,
        string TimeZoneId
    );

    public record GetPublicBranchesQuery() : IQuery<IReadOnlyCollection<BranchSummary>>;
}
