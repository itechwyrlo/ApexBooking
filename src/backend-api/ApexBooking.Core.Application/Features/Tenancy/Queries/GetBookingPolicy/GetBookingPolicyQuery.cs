using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetBookingPolicy
{
    public record BookingPolicyDto(
        BookingConfirmationMode BookingConfirmationMode,
        int MinAdvanceBookingHours,
        int MaxAdvanceBookingDays,
        int CancellationCutoffHours,
        CancellationPolicy LateCancellationPolicy,
        bool NotifyBookingConfirmed,
        bool NotifyBookingCancelled,
        bool NotifyBookingReminder,
        bool NotifyNewCustomer,
        int ReminderHoursBefore
    );

    public record GetBookingPolicyQuery() : IQuery<BookingPolicyDto>;
}
