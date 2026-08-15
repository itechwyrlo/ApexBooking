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
    RefundSucceeded,
    RefundFailed,
    RefundReviewNeeded,
    StaffCreated,
    StaffDeactivated,

    // Super Admin
    TenantRequestSubmitted,
    TenantRequestApproved,
    TenantRequestRejected,
    TrialReminderSent,
    // Renamed from TrialExpiredSuspended: expiry does not suspend the tenant (see TrialPeriod's doc
    // comment) — the old name baked in a business rule that isn't true.
    TrialExpired,
    BackgroundJobFailed
}
