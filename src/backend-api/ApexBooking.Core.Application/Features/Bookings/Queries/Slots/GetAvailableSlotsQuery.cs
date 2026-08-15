using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Services.BookingEngine;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.Slots
{
    // UnavailableReason is a friendly, ready-to-display message — null whenever Slots is non-empty.
    public record AvailableSlotsResult(
        IReadOnlyCollection<AvailableSlotResponse> Slots,
        string? UnavailableReason
    );

   public record GetAvailableSlotsQuery(
        Guid BranchId,
        Guid StaffId,
        Guid ServiceId,
        DateOnly TargetDate
    ) : IQuery<AvailableSlotsResult>;
}
