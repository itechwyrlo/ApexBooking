using FluentValidation;
using ApexBooking.Core.Application.Features.Auth.Commands.AccountVerification;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for AccountVerificationCommand - validates email verification token
/// </summary>
public class AccountVerificationCommandValidator : AbstractValidator<AccountVerificationCommand>
{
    public AccountVerificationCommandValidator()
    {
        RuleFor(x => x.token)
            .NotEmpty().WithMessage("Verification token is required")
            .MaximumLength(1000).WithMessage("Verification token is too long");
    }
}
