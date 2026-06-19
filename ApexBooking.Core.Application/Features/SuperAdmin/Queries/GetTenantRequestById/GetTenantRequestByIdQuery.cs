using System;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequestById;

public sealed record GetTenantRequestByIdQuery(Guid Id)
    : IQuery<TenantRequestDetailDto>;
