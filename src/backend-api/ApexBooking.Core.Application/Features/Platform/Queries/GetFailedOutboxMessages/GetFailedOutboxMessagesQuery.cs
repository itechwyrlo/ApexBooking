using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Platform.Queries.GetFailedOutboxMessages
{
    public record FailedOutboxMessageSummary(
        Guid Id,
        string EventType,
        string? LastError,
        int RetryCount,
        DateTime OccurredAtUtc
    );

    public record GetFailedOutboxMessagesQuery() : IQuery<IReadOnlyCollection<FailedOutboxMessageSummary>>;
}
