using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Services.Commands.UpdateService
{
    public record UpdateServiceCommand(
        Guid ServiceId,
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price,
        string CurrencyCode,
        int BufferBeforeMinutes,
        int BufferAfterMinutes,
        int? MinAdvanceBookingHoursOverride = null
    ) : ICommand;
}