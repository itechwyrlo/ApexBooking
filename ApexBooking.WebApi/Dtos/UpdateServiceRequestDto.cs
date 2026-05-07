using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.WebApi.Dtos
{
    public sealed class UpdateServiceRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public List<Guid> ResourceIds { get; set; } = new();
        public int BufferBeforeMinutes { get; set; }
        public int BufferAfterMinutes { get; set; }
        public int? MinAdvanceBookingHours { get; set; }
        public int? MaxAdvanceBookingDays { get; set; }
    }
}