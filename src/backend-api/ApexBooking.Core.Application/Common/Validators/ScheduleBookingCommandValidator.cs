using ApexBooking.Core.Application.Features.Bookings.Commands.ScheduleBooking;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for ScheduleBookingCommand - validates identifiers, customer contact, and scheduling fields
/// </summary>
public class ScheduleBookingCommandValidator : AbstractValidator<ScheduleBookingCommand>
{
    public ScheduleBookingCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty().WithMessage("Branch id is required");

        RuleFor(x => x.StaffId)
            .NotEmpty().WithMessage("Staff id is required");

        RuleFor(x => x.ServiceId)
            .NotEmpty().WithMessage("Service id is required");

        RuleFor(x => x.CustomerFirstName)
            .NotEmpty().WithMessage("Customer first name is required");

        RuleFor(x => x.CustomerLastName)
            .NotEmpty().WithMessage("Customer last name is required");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.CustomerEmail) || !string.IsNullOrWhiteSpace(x.CustomerPhone))
            .WithMessage("Either a customer email or phone number is required")
            .OverridePropertyName("CustomerEmail");

        RuleFor(x => x.ScheduledDate)
            .NotEmpty().WithMessage("Scheduled date is required");
    }
}
