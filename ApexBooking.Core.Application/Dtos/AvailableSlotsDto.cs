using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed class AvailableSlotsDto
    {
        public Guid ServiceId { get; init; }
        public Guid ResourceId { get; init; }

        /// <summary>ISO 8601: YYYY-MM-DD. TR-15.1</summary>
        public DateOnly Date { get; init; }

        /// <summary>
        /// Duration of each slot in minutes. Returned so the client can
        /// compute the slot end time without a second call.
        /// </summary>
        public int DurationMinutes { get; init; }

        /// <summary>
        /// Available slot start times in HH:mm 24-hour format. TR-15.2
        /// Empty list means no slots are open on this date.
        /// </summary>
        public IReadOnlyList<string> AvailableSlots { get; init; } = [];
    }
}