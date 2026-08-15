using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.BookingPolicy
{
    public record UpdateBookingPolicyCommand(
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
    ) : ICommand;
}