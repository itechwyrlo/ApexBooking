using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos
{
    public record PublicServiceDto(
        Guid ServiceId,
        string Name,
        string? Description,
        int DurationMinutes,
        decimal Price,
        string CurrencyCode
    );
}