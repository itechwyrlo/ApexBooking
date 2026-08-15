using ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfile;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
