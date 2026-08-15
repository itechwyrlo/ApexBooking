namespace ApexBooking.Core.Domain.Enums;

/// <summary>Status of a customer→business payment for a booking (10 §3). Never a Booking status (ADR-050).</summary>
public enum BookingPaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

/// <summary>Status of a tenant's ApexBooking subscription (Flow A, 10 §3/§4).</summary>
public enum TenantSubscriptionStatus
{
    Trialing,
    Active,
    PastDue,
    Canceled
}

/// <summary>Connection status of a tenant's PayMongo account for receiving customer payments (Flow B, 10 §3).</summary>
public enum TenantPaymentAccountStatus
{
    Disconnected,
    Connected
}
