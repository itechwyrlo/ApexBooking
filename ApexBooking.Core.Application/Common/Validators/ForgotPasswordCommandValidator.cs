using FluentValidation;
using ApexBooking.Core.Application.Features.Auth.Commands.ForgotPassword;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for ForgotPasswordCommand - validates email
/// </summary>
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters");
    }
}
