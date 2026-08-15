using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicServicesByBranch
{
    // Dedicated to the public booking wizard — deliberately narrower than the authenticated
    // dashboard's ServiceCatalogSummary (no IsActive/CreatedAt/MinAdvanceBookingHoursOverride;
    // this endpoint only ever returns active services already filtered server-side).
    public record PublicServiceSummary(
        Guid ServiceId,
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price,
        string CurrencyCode
    );

    public record GetPublicServicesByBranchQuery(Guid BranchId) : IQuery<IReadOnlyCollection<PublicServiceSummary>>;
}
