using ApexBooking.Core.Application.Features.Public.Commands.SubmitTenantRequest;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class SubmitTenantRequestCommandValidator : AbstractValidator<SubmitTenantRequestCommand>
{
    public SubmitTenantRequestCommandValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(200);

        RuleFor(x => x.OwnerFullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200);

        RuleFor(x => x.OwnerEmail)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.OwnerPhone)
            .MaximumLength(50);

        RuleFor(x => x.Plan)
            .NotEmpty().WithMessage("Plan is required.")
            .Must(p => p.Equals("Basic", StringComparison.OrdinalIgnoreCase) ||
                       p.Equals("Professional", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Plan must be 'Basic' or 'Professional'.");

        RuleFor(x => x.Message)
            .MaximumLength(1000);
    }
}
