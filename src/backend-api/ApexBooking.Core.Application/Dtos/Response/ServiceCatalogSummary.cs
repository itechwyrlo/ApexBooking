using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record ServiceCatalogSummary(
        Guid ServiceId,
        string Name,
        string? Description,
        int DurationMinutes,
        int BufferBeforeMinutes,
        int BufferAfterMinutes,
        decimal Price,
        string CurrencyCode,
        int? MinAdvanceBookingHoursOverride,
        bool IsActive,
        DateTime CreatedAt
    );
}