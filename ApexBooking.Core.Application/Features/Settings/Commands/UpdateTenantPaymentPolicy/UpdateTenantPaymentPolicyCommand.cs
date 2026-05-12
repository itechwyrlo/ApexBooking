using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Settings.Commands.UpdateTenantPaymentPolicy;

public sealed record UpdateTenantPaymentPolicyCommand(
    bool? PaymentRequired,
    bool? DepositOnly,
    DepositType? DepositType,
    decimal? DepositValue,
    decimal? RefundPercent
) : ICommand<BaseResponse<TenantPaymentPolicyDto>>;
