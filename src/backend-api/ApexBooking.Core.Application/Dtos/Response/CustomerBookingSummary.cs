using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record CustomerBookingSummary(
        Guid BookingId,
        string BookingReference,
        string ServiceName,
        string StaffName,
        string BranchName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        BookingStatus Status,
        bool RequiresUpfrontPayment,
        decimal AmountDue,
        string CurrencyCode,
        PaymentConfirmationMethod? PaymentConfirmedVia,
        DateTime CreatedAt
    );
}
