using FluentValidation;
using ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateOrganization;

namespace ApexBooking.Core.Application.Common.Validators;

public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MinimumLength(3).WithMessage("Slug must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Slug cannot exceed 50 characters.")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, digits, and hyphens.");

        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(256).WithMessage("Business name cannot exceed 256 characters.");

        RuleFor(x => x.OwnerFullName)
            .NotEmpty().WithMessage("Owner full name is required.")
            .MaximumLength(200).WithMessage("Owner full name cannot exceed 200 characters.");

        RuleFor(x => x.OwnerEmail)
            .NotEmpty().WithMessage("Owner email is required.")
            .EmailAddress().WithMessage("Owner email must be a valid email address.")
            .MaximumLength(256).WithMessage("Owner email cannot exceed 256 characters.");

        RuleFor(x => x.OwnerPhone)
            .NotEmpty().WithMessage("Owner phone is required.")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone must be a valid E.164 format.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Admin password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
