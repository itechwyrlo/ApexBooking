using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.InitiateBooking
{
    public record InitiateBookingCommand(
        Guid BranchId,
        Guid StaffId,
        Guid ServiceId,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        string CustomerFirstName,
        string CustomerLastName,
        string CustomerEmail,
        string CustomerPhone,
        string? CustomerNotes
    ) : ICommand<BookingInitiationResult>;
}