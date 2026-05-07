using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos
{
    public record BookingDto(
     Guid BookingId,
     string BookingReference,
     Guid ServiceId,
     string ServiceName,
     Guid ResourceId,
     string ResourceName,
     Guid? UserId,
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