using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record PublicBookingDto(
        Guid BookingId,
        string BookingReference,
        string ServiceName,
        string ResourceName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        TimeOnly ScheduledEndTime,
        BookingStatus Status,
        string GuestFirstName
    );
}