using ApexBooking.Core.Application.Features.Settings.Commands.UpdateTenantPaymentPolicy;
using ApexBooking.Core.Domain.Entities;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class UpdateTenantPaymentPolicyCommandValidator
    : AbstractValidator<UpdateTenantPaymentPolicyCommand>
{
    public UpdateTenantPaymentPolicyCommandValidator()
    {
        RuleFor(x => x.DepositValue)
            .GreaterThanOrEqualTo(0).WithMessage("Deposit value must be non-negative.")
            .When(x => x.DepositValue.HasValue);

        RuleFor(x => x.DepositValue)
            .LessThanOrEqualTo(100).WithMessage("Deposit percentage cannot exceed 100.")
            .When(x => x.DepositValue.HasValue && x.DepositType == DepositType.Percentage);

        RuleFor(x => x.RefundPercent)
            .InclusiveBetween(0, 100).WithMessage("Refund percent must be between 0 and 100.")
            .When(x => x.RefundPercent.HasValue);
    }
}
