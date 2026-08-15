using ApexBooking.Core.Application.Features.Bookings.Commands.MarkBookingNoShow;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for MarkBookingNoShowCommand - validates identifier is present
/// </summary>
public class MarkBookingNoShowCommandValidator : AbstractValidator<MarkBookingNoShowCommand>
{
    public MarkBookingNoShowCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("Booking id is required");
    }
}
