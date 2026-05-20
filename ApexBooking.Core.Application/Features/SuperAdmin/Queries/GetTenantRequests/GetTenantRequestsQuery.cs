using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequests;

public sealed record GetTenantRequestsQuery(string? Status = null)
    : IQuery<IEnumerable<TenantRequestDto>>;
