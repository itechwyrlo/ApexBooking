using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentPolicy
{
    public record PaymentPolicyDto(
        PaymentRequirementType RequirementType,
        DepositType DepositType,
        decimal DepositValue,
        decimal OnTimeRefundPercent,
        decimal LateCancellationRefundPercent,
        int RefundReviewDeadlineDays,
        bool RefundEnabled
    );

    public record GetPaymentPolicyQuery() : IQuery<PaymentPolicyDto>;
}
