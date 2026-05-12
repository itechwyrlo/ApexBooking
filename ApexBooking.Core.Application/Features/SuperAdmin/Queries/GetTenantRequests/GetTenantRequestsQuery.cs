using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.Core.Application.Dtos;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequests;

public sealed record GetTenantRequestsQuery(string? Status = null)
    : IQuery<BaseResponse<IEnumerable<TenantRequestDto>>>;
