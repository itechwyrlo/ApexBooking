using ApexBooking.Core.Application.Features.Bookings.Commands.AdmitBooking;
using FluentValidation;

namespace ApexBooking.Core.Application.Common.Validators;

/// <summary>
/// Validator for AdmitBookingCommand - validates identifier is present
/// </summary>
public class AdmitBookingCommandValidator : AbstractValidator<AdmitBookingCommand>
{
    public AdmitBookingCommandValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("Booking id is required");
    }
}
