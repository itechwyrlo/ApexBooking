using ApexBooking.Core.Application.Features.TimeOffs.Commands.ApproveTimeOff;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for ApproveTimeOffCommand - validates identifier is present
/// </summary>
public class ApproveTimeOffCommandValidator : AbstractValidator<ApproveTimeOffCommand>
{
    public ApproveTimeOffCommandValidator()
    {
        RuleFor(x => x.TimeOffRequestId)
            .NotEmpty().WithMessage("Time-off request id is required");
    }
}
