using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.BookingEngine
{
    public record AvailableSlotResponse(
        string TimeString, // e.g., "09:00 AM" formatted for clean UI rendering
        TimeOnly RawTime   // Primitive data tracker for execution binding
    );
}