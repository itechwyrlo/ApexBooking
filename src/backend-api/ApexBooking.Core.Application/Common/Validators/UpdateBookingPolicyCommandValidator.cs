using ApexBooking.Core.Application.Features.Tenancy.Commands.BookingPolicy;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for UpdateBookingPolicyCommand - validates scheduling windows and enum values
/// </summary>
public class UpdateBookingPolicyCommandValidator : AbstractValidator<UpdateBookingPolicyCommand>
{
    public UpdateBookingPolicyCommandValidator()
    {
        RuleFor(x => x.BookingConfirmationMode)
            .IsInEnum().WithMessage("Booking confirmation mode must be a valid value");

        RuleFor(x => x.MinAdvanceBookingHours)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum advance booking hours cannot be negative");

        RuleFor(x => x.MaxAdvanceBookingDays)
            .GreaterThan(0).WithMessage("Maximum advance booking days must be greater than zero");

        RuleFor(x => x.CancellationCutoffHours)
            .GreaterThanOrEqualTo(0).WithMessage("Cancellation cutoff hours cannot be negative");

        RuleFor(x => x.LateCancellationPolicy)
            .IsInEnum().WithMessage("Late cancellation policy must be a valid value");

        RuleFor(x => x.ReminderHoursBefore)
            .GreaterThanOrEqualTo(0).WithMessage("Reminder hours before cannot be negative");
    }
}
