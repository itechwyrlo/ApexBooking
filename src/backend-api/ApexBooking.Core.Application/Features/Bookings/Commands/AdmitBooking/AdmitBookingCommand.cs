using System;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.AdmitBooking
{
    // Manual arrival admission — staff-initiated (e.g. a walk-in they just booked, or a customer
    // without their QR pass), as opposed to ScanArrivalCommand which validates a signed token.
    public record AdmitBookingCommand(Guid BookingId) : ICommand<ScanArrivalResult>;
}
