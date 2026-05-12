using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetTenantPaymentPolicy;

public sealed record GetTenantPaymentPolicyQuery() : IQuery<BaseResponse<TenantPaymentPolicyDto>>;
