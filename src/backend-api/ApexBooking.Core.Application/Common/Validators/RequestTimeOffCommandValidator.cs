using ApexBooking.Core.Application.Features.TimeOffs.Commands.RequestTimeOff;
using ApexBooking.Core.Domain.Enums;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for RequestTimeOffCommand - validates date range and partial-day time window
/// </summary>
public class RequestTimeOffCommandValidator : AbstractValidator<RequestTimeOffCommand>
{
    public RequestTimeOffCommandValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("A valid time-off type is required");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date cannot be before the start date");

        RuleFor(x => x.StartTime)
            .NotNull().WithMessage("Start time is required for partial-day time off")
            .When(x => x.Type == TimeOffType.PartialDay);

        RuleFor(x => x.EndTime)
            .NotNull().WithMessage("End time is required for partial-day time off")
            .When(x => x.Type == TimeOffType.PartialDay);

        RuleFor(x => x)
            .Must(x => x.StartTime!.Value < x.EndTime!.Value)
            .WithMessage("Start time must be before end time")
            .OverridePropertyName("EndTime")
            .When(x => x.Type == TimeOffType.PartialDay && x.StartTime is not null && x.EndTime is not null);

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
