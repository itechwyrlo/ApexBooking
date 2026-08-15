using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class BookingPolicy
{
    public BookingPolicyId BookingPolicyId { get; protected set; } = default!;
    public TenantId TenantId { get; protected set; } = default!;
    public BookingConfirmationMode BookingConfirmationMode { get; private set; }
    public int MinAdvanceBookingHours { get; private set; }
    public int MaxAdvanceBookingDays { get; private set; }
    public int CancellationCutoffHours { get; private set; }
    public CancellationPolicy LateCancellationPolicy { get; private set; }
    public bool NotifyBookingConfirmed { get; private set; }
    public bool NotifyBookingCancelled { get; private set; }
    public bool NotifyBookingReminder { get; private set; }
    public bool NotifyNewCustomer { get; private set; }
    public int ReminderHoursBefore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation property
    public Tenant Tenant { get; private set; } = default!;

    protected BookingPolicy() { }

    [SetsRequiredMembers]
    public BookingPolicy(TenantId tenantId)
    {
        BookingPolicyId = new BookingPolicyId(Guid.NewGuid());
        TenantId = tenantId;
        BookingConfirmationMode = BookingConfirmationMode.Automatic;
        MinAdvanceBookingHours = 48;
        MaxAdvanceBookingDays = 60;
        CancellationCutoffHours = 24;
        LateCancellationPolicy = CancellationPolicy.NoRefund;
        NotifyBookingConfirmed = true;
        NotifyBookingCancelled = true;
        NotifyBookingReminder = true;
        NotifyNewCustomer = true;
        ReminderHoursBefore = 24;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static BookingPolicy CreateDefault(TenantId tenantId)
    {
        return new BookingPolicy(tenantId);
    }

    public void UpdateSettings(
        BookingConfirmationMode? bookingConfirmationMode = null,
        int? minAdvanceBookingHours = null,
        int? maxAdvanceBookingDays = null,
        int? cancellationCutoffHours = null,
        CancellationPolicy? lateCancellationPolicy = null,
        bool? notifyBookingConfirmed = null,
        bool? notifyBookingCancelled = null,
        bool? notifyBookingReminder = null,
        bool? notifyNewCustomer = null,
        int? reminderHoursBefore = null)
    {
        if (bookingConfirmationMode.HasValue)
            BookingConfirmationMode = bookingConfirmationMode.Value;

        if (minAdvanceBookingHours.HasValue && minAdvanceBookingHours.Value >= 0)
            MinAdvanceBookingHours = minAdvanceBookingHours.Value;

        if (maxAdvanceBookingDays.HasValue && maxAdvanceBookingDays.Value > 0)
            MaxAdvanceBookingDays = maxAdvanceBookingDays.Value;

        if (cancellationCutoffHours.HasValue && cancellationCutoffHours.Value >= 0)
            CancellationCutoffHours = cancellationCutoffHours.Value;

        if (lateCancellationPolicy.HasValue)
            LateCancellationPolicy = lateCancellationPolicy.Value;

        if (notifyBookingConfirmed.HasValue)
            NotifyBookingConfirmed = notifyBookingConfirmed.Value;

        if (notifyBookingCancelled.HasValue)
            NotifyBookingCancelled = notifyBookingCancelled.Value;

        if (notifyBookingReminder.HasValue)
            NotifyBookingReminder = notifyBookingReminder.Value;

        if (notifyNewCustomer.HasValue)
            NotifyNewCustomer = notifyNewCustomer.Value;

        if (reminderHoursBefore.HasValue && reminderHoursBefore.Value > 0)
            ReminderHoursBefore = reminderHoursBefore.Value;

        UpdatedAt = DateTime.UtcNow;
    }
}




