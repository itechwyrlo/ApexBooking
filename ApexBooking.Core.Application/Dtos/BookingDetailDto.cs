using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record BookingDetailDto(
        Guid BookingId,
        string BookingReference,
        Guid ServiceId,
        string ServiceName,
        Guid ResourceId,
        string ResourceName,
        GuestDto? Guest,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        TimeOnly ScheduledEndTime,
        int DurationMinutes,
        BookingStatus Status,
        BookingConfirmationMode ConfirmationMode,
        decimal PriceSnapshot,
        string CurrencyCode,
        string? CustomerNotes,
        string? CancellationReason,
        DateTime? CancelledAt,
        DateTime CreatedAt
    );
}
