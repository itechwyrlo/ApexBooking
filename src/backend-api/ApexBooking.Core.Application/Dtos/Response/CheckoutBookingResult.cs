namespace ApexBooking.Core.Application.Dtos.Response
{
    public record CheckoutBookingResult(
        Guid BookingId,
        string BookingReference,
        DateTime CompletedAt,
        decimal AmountSettled,
        string CurrencyCode
    );
}
