using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServicesByBranch
{
    public record GetServicesByBranchQuery(Guid BranchId) : IQuery<IReadOnlyCollection<ServiceCatalogSummary>>;
}
