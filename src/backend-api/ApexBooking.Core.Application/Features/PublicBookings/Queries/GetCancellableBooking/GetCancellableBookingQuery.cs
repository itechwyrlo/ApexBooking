using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetCancellableBooking
{
    public record CancellableBookingDto(
        string BookingReference,
        string ServiceName,
        string StaffName,
        string BranchName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        bool CanCancelOnline,
        string? UnavailableReason,
        bool IsRefundEligible
    );

    public record GetCancellableBookingQuery(string Token) : IQuery<CancellableBookingDto>;
}
