using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Services;

namespace ApexBooking.Core.Application.Features.Platform.Queries.GetFailedOutboxMessages
{
    public class GetFailedOutboxMessagesHandler
        : IQueryHandler<GetFailedOutboxMessagesQuery, IReadOnlyCollection<FailedOutboxMessageSummary>>
    {
        private readonly IOutboxStore _outboxStore;

        public GetFailedOutboxMessagesHandler(IOutboxStore outboxStore)
        {
            _outboxStore = outboxStore;
        }

        public async Task<IReadOnlyCollection<FailedOutboxMessageSummary>> Handle(
            GetFailedOutboxMessagesQuery query,
            CancellationToken cancellationToken)
        {
            var failed = await _outboxStore.GetFailedAsync(cancellationToken);

            return failed
                .Select(m => new FailedOutboxMessageSummary(m.Id, m.EventType, m.LastError, m.RetryCount, m.OccurredAtUtc))
                .ToList();
        }
    }
}
