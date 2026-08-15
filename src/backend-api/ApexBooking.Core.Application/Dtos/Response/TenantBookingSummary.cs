using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record TenantBookingSummary(
        Guid BookingId,
        string BookingReference,
        string CustomerName,
        string? CustomerPhone,
        string ServiceName,
        string StaffName,
        string BranchName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        int DurationMinutes,
        BookingStatus Status,
        bool RequiresUpfrontPayment,
        decimal AmountDue,
        string CurrencyCode,
        PaymentConfirmationMethod? PaymentConfirmedVia,
        DateTime? CheckedInAt,
        DateTime? ServiceCompletedAt,
        DateTime? CancelledAt,
        string? CancellationReason,
        DateTime? NoShowAt,
        Guid CustomerId,
        Guid StaffId,
        DateTime CreatedAt,
        decimal AmountPaidOnline,
        decimal RemainingBalance
    );
}
