using ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentPolicy;
using ApexBooking.Core.Domain.Enums;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for UpdatePaymentPolicyCommand - validates deposit and refund bounds
/// </summary>
public class UpdatePaymentPolicyCommandValidator : AbstractValidator<UpdatePaymentPolicyCommand>
{
    public UpdatePaymentPolicyCommandValidator()
    {
        RuleFor(x => x.RequirementType)
            .IsInEnum().WithMessage("Requirement type must be a valid value");

        RuleFor(x => x.DepositType)
            .IsInEnum().WithMessage("Deposit type must be a valid value");

        RuleFor(x => x.DepositValue)
            .GreaterThanOrEqualTo(0).WithMessage("Deposit value cannot be a negative amount");

        RuleFor(x => x.DepositValue)
            .LessThanOrEqualTo(100).WithMessage("A percentage-based deposit requirement cannot exceed 100%")
            .When(x => x.DepositType == DepositType.Percentage);

        RuleFor(x => x.OnTimeRefundPercent)
            .InclusiveBetween(0, 100).WithMessage("On-time refund percentage must be between 0% and 100%");

        RuleFor(x => x.LateCancellationRefundPercent)
            .InclusiveBetween(0, 100).WithMessage("Late cancellation refund percentage must be between 0% and 100%");
    }
}
