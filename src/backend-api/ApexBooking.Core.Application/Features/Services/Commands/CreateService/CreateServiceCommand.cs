using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Services.Commands.CreateService
{
    public record CreateServiceCommand(
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price,
        string CurrencyCode,
        int BufferBeforeMinutes = 0,
        int BufferAfterMinutes = 0,
        int? MinAdvanceBookingHoursOverride = null
    ) : ICommand<Guid>;

}