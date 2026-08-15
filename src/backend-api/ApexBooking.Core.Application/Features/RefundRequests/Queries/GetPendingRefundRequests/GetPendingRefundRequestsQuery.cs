using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests
{
    public record RefundRequestSummaryDto(
        Guid Id,
        Guid BookingId,
        string BookingReference,
        string CustomerName,
        decimal RequestedAmount,
        decimal AmountPaid,
        string? PayMongoPaymentId,
        string CurrencyCode,
        RefundRequestStatus Status,
        string? RejectionReason,
        string CustomerEwalletProvider,
        string CustomerEwalletNumber,
        string CustomerEwalletName,
        string? ReceiptUrl,
        DateTime CreatedAt,
        DateTime DueDate
    );

    public record GetPendingRefundRequestsQuery(int PageNumber = 1, int PageSize = 10) : IQuery<QueryResult<RefundRequestSummaryDto>>;
}
