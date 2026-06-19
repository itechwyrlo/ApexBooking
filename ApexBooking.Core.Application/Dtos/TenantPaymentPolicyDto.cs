namespace ApexBooking.Core.Application.Dtos;

public sealed record TenantPaymentPolicyDto(
    bool PaymentRequired,
    bool DepositOnly,
    string DepositType,
    decimal DepositValue,
    decimal RefundPercent
);
