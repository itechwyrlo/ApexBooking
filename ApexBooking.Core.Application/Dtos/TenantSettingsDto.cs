using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record TenantSettingsDto(
        string BookingConfirmationMode,
        int MinAdvanceBookingHours,
        int MaxAdvanceBookingDays,
        int CancellationCutoffHours,
        string LateCancellationPolicy,
        bool GuestBookingEnabled,
        bool NotifyBookingConfirmed,
        bool NotifyBookingCancelled,
        bool NotifyBookingReminder,
        bool NotifyNewCustomer,
        int ReminderHoursBefore
    );
}