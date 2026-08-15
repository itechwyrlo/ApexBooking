using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ScheduleBooking
{
    // Admin-dashboard / walk-in booking creation — skips the online-payment step entirely
    // (Tenant.ScheduleBooking always books as requiresUpfrontPayment: false; staff handle
    // payment collection in person if the tenant's PaymentPolicy calls for it).
    public record ScheduleBookingCommand(
        Guid BranchId,
        Guid StaffId,
        Guid ServiceId,
        string CustomerFirstName,
        string CustomerLastName,
        string? CustomerEmail,
        string? CustomerPhone,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        string? CustomerNotes,
        // True only when the caller picked a staff member's immediate "available now" slot —
        // not for a later-today slot on an otherwise-busy staff member. See Tenant.PlaceBooking.
        bool AdmitImmediately
    ) : ICommand<Guid>;
}
