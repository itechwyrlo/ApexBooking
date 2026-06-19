namespace ApexBooking.Core.Application.Dtos
{
    public sealed record ClientSummaryDto(
        string Email,
        string FullName,
        string? Phone,
        int TotalBookings,
        DateOnly? LastVisit,
        decimal TotalSpent,
        string CurrencyCode
    );
}
