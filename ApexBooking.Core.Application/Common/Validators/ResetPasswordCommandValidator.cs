using FluentValidation;
using ApexBooking.Core.Application.Features.Auth.Commands.ResetPassword;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for ResetPasswordCommand - validates reset tokens and passwords
/// </summary>
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
        
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required")
            .MaximumLength(1000).WithMessage("Token is too long");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters")
            .MaximumLength(256).WithMessage("New password cannot exceed 256 characters")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter")
            .Matches(@"[0-9]").WithMessage("New password must contain at least one digit");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Confirm password must match the new password");
    }
}
