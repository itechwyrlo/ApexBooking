using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record ClientBookingDto(
        Guid BookingId,
        string BookingReference,
        string ServiceName,
        string ResourceName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        TimeOnly ScheduledEndTime,
        BookingStatus Status,
        decimal PriceSnapshot,
        string CurrencyCode
    );
}
