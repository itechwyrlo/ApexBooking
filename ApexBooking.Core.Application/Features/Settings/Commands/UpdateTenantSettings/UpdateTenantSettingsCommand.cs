using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Features.Settings.Commands.UpdateTenantSettings;

public sealed record UpdateTenantSettingsCommand(
    BookingConfirmationMode? BookingConfirmationMode,
    int? MinAdvanceBookingHours,
    int? MaxAdvanceBookingDays,
    int? CancellationCutoffHours,
    CancellationPolicy? LateCancellationPolicy,
    bool? GuestBookingEnabled,
    bool? NotifyBookingConfirmed,
    bool? NotifyBookingCancelled,
    bool? NotifyBookingReminder,
    bool? NotifyNewCustomer,
    int? ReminderHoursBefore
) : ICommand<TenantSettingsDto>;