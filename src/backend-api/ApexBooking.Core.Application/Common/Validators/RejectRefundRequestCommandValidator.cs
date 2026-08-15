using ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

public class RejectRefundRequestCommandValidator : AbstractValidator<RejectRefundRequestCommand>
{
    public RejectRefundRequestCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required when rejecting a refund request.")
            .MaximumLength(500);
    }
}
