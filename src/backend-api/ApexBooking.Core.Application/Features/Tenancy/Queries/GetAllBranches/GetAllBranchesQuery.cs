using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllBranches
{
    public record BranchAdminSummary(
        Guid BranchId,
        string BranchName,
        string Street,
        string? Barangay,
        string City,
        string Province,
        string ZipCode,
        string TimeZoneId,
        bool IsActive
    );

    public record GetAllBranchesQuery() : IQuery<IReadOnlyCollection<BranchAdminSummary>>;
}
