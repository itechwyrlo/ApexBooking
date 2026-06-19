namespace ApexBooking.Core.Application.Dtos
{
    public sealed record ClientDetailDto(
        string Email,
        string FullName,
        string? Phone,
        int TotalBookings,
        DateOnly? LastVisit,
        decimal TotalSpent,
        string CurrencyCode,
        IReadOnlyList<ClientBookingDto> Bookings
    );
}
