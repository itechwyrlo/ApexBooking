namespace ApexBooking.Core.Application.Dtos
{
    public sealed class ServiceDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int DurationMinutes { get; init; }
        public int BufferBeforeMinutes { get; init; }
        public int BufferAfterMinutes { get; init; }
        public decimal Price { get; init; }
        public string CurrencyCode { get; init; } = string.Empty;
        public int? MinAdvanceBookingHours { get; init; }
        public int? MaxAdvanceBookingDays { get; init; }
        public bool IsActive { get; init; }
        public List<Guid> ResourceIds { get; init; } = new();
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
