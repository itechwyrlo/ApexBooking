using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetTenantPaymentPolicy;

public sealed record GetTenantPaymentPolicyQuery() : IQuery<TenantPaymentPolicyDto>;
