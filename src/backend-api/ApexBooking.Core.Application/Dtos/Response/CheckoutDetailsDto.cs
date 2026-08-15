namespace ApexBooking.Core.Application.Dtos.Response
{
    public record CheckoutDetailsDto(
        Guid BookingId,
        string BookingReference,
        string CustomerName,
        string? CustomerEmail,
        string? CustomerPhone,
        string ServiceName,
        string StaffName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        decimal AmountPaidOnline,
        decimal RemainingBalance,
        string CurrencyCode
    );
}
