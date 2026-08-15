using ApexBooking.Core.Application.Features.TimeOffs.Commands.RejectTimeOff;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for RejectTimeOffCommand - validates identifier is present
/// </summary>
public class RejectTimeOffCommandValidator : AbstractValidator<RejectTimeOffCommand>
{
    public RejectTimeOffCommandValidator()
    {
        RuleFor(x => x.TimeOffRequestId)
            .NotEmpty().WithMessage("Time-off request id is required");
    }
}
