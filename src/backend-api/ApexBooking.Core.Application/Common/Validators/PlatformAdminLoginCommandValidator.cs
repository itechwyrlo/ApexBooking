using ApexBooking.Core.Application.Features.Authentication.PlatformAdminLogin;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class PlatformAdminLoginCommandValidator : AbstractValidator<PlatformAdminLoginCommand>
{
    public PlatformAdminLoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters")
            .Must(e => !e.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Email contains invalid characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(256).WithMessage("Password cannot exceed 256 characters");
    }
}
