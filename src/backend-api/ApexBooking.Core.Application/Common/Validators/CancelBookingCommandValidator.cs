using ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for CancelBookingCommand - validates identifiers and required cancellation reason
/// </summary>
public class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("Booking id is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A cancellation reason is required")
            .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters");
    }
}
