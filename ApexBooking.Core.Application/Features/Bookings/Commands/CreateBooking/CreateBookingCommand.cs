using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CreateBooking
{
    public sealed record CreateBookingCommand(
        Guid ServiceId,
        Guid ResourceId,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        string? CustomerNotes
    ) : ICommand<BaseResponse<BookingDto>>;
}