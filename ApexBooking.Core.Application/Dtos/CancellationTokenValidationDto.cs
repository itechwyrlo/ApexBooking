using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record CancellationTokenValidationDto(
        Guid BookingId,
        string BookingReference,
        string ServiceName,
        string ResourceName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        BookingStatus Status,
        string GuestFirstName,
        string GuestLastName,
        decimal PriceSnapshot,
        string CurrencyCode,
        decimal RefundPercent,
        bool DepositOnly,
        DepositType DepositType,
        decimal DepositValue
    );
}
