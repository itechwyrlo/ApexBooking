using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Domain.Services;

// Data access port for the ProcessedPaymentEvent idempotency ledger — same shape as
// IRefundRequestStore: ProcessedPaymentEvent is not an IAggregateRoot, so there's no generic
// repository for it.
public interface IProcessedPaymentEventStore
{
    Task<bool> ExistsAsync(string payMongoEventId, CancellationToken cancellationToken = default);

    // Tracks a new ledger row on the ambient DbContext WITHOUT saving — deliberately NOT symmetric
    // with IRefundRequestStore.AddAsync (which calls SaveChangesAsync immediately). The caller
    // must persist this in the SAME IUnitOfWork.CompleteAsync() call that saves the booking's
    // payment-confirmation write (see ProcessPaymentWebhookCommandHandler), so the two commit in
    // one transaction — never mark an event processed before the payment state change it belongs
    // to has actually been saved.
    void Add(ProcessedPaymentEvent processedPaymentEvent);
}
