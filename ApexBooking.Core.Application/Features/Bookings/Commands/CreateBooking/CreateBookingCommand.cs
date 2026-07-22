using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CreateBooking
{
    public sealed record CreateBookingCommand(
        string TenantSlug,
        Guid ServiceId,
        Guid? StaffId,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        string GuestFirstName,
        string GuestLastName,
        string GuestEmail,
        string? GuestPhone,
        string? CustomerNotes
    ) : ICommand<BookingDetailDto>;
}
