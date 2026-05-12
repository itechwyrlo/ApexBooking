using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequestById;

public sealed record GetTenantRequestByIdQuery(Guid Id)
    : IQuery<BaseResponse<TenantRequestDetailDto>>;
