using System;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetBookingStatusByTicket
{
    public record PublicBookingStatusDto(
        Guid BookingId,
        string BookingReference,
        BookingStatus Status,
        string ServiceName,
        string StaffName,
        string BranchName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        bool RequiresUpfrontPayment,
        decimal AmountDue,
        string CurrencyCode
    );

    public record GetBookingStatusByTicketQuery(string TicketToken) : IQuery<PublicBookingStatusDto>;
}
