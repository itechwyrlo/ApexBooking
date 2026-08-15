using ApexBooking.Core.Application.Features.TenantRequest.Commands.Reject;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for a Super Admin rejection. The reason is mandatory because it is sent verbatim
/// to the business owner in the rejection email.
/// </summary>
public class RejectTenantRequestCommandValidator : AbstractValidator<RejectTenantRequestCommand>
{
    public RejectTenantRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Request ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A rejection reason is required.")
            .MaximumLength(1000).WithMessage("Rejection reason cannot exceed 1000 characters.");
    }
}
