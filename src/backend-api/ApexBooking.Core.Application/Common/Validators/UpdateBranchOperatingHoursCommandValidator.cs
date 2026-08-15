using ApexBooking.Core.Application.Features.Tenancy.Commands.Branches;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for UpdateBranchOperatingHoursCommand - validates each day's start/end time ordering
/// </summary>
public class UpdateBranchOperatingHoursCommandValidator : AbstractValidator<UpdateBranchOperatingHoursCommand>
{
    public UpdateBranchOperatingHoursCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("Branch id is required");

        RuleFor(x => x.OperatingHours)
            .NotEmpty().WithMessage("At least one day's operating hours must be provided");

        RuleForEach(x => x.OperatingHours)
            .Must(item => item.IsOff || item.StartTime < item.EndTime)
            .WithMessage("Start time must be earlier than end time for a day that is open");
    }
}
