using ApexBooking.Core.Application.Features.SuperAdmin.Commands.AssignExistingUser;
using ApexBooking.Core.Domain.Entities;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class AssignExistingUserCommandValidator : AbstractValidator<AssignExistingUserCommand>
{
    public AssignExistingUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => Enum.TryParse<UserRole>(r, ignoreCase: true, out _))
            .WithMessage("Role must be one of: TenantAdmin, Manager, Staff, Customer.");
    }
}
