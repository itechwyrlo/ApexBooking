using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentPolicy
{
    public record UpdatePaymentPolicyCommand(
        PaymentRequirementType RequirementType,
        DepositType DepositType,
        decimal DepositValue,
        decimal OnTimeRefundPercent,
        decimal LateCancellationRefundPercent,
        int RefundReviewDeadlineDays,
        bool RefundEnabled
    ) : ICommand;
}