using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog
{
    public record RefundLogEntryDto(
        Guid Id,
        string BookingReference,
        decimal Amount,
        string CurrencyCode,
        RefundRequestStatus Status,
        DateTime ProcessedAt
    );

    public record GetRefundLogQuery(int Limit = 20) : IQuery<IReadOnlyCollection<RefundLogEntryDto>>;
}
