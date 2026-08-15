using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus
{
    public record RefundStatusDto(
        string BookingReference,
        RefundRequestStatus? Status,
        decimal? Amount,
        string CurrencyCode,
        string? BusinessContactPhoneNumber,
        string? ReceiptUrl
    );

    public record GetRefundStatusQuery(string Token) : IQuery<RefundStatusDto>;
}
