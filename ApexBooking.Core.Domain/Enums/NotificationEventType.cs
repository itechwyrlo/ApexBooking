namespace ApexBooking.Core.Domain.Enums;

public enum NotificationEventType
{
    // Tenant Admin
    BookingCreated,
    BookingConfirmed,
    BookingCancelled,
    BookingCompleted,
    BookingNoShow,
    BookingPendingPayment,
    PaymentCaptured,
    StaffCreated,
    StaffDeactivated,

    // Super Admin
    TenantRequestSubmitted,
    TenantRequestApproved,
    TenantRequestRejected,
    TrialReminderSent,
    TrialExpiredSuspended,
    BackgroundJobFailed
}
