using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking
{
    public sealed record CancelBookingCommand(
        Guid BookingId,
        string? Reason
    ) : ICommand<BaseResponse<bool>>;
}