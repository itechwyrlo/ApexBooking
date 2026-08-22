using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Domain.Services;

/// <summary>
/// Data access port for <see cref="OutboxMessage"/> rows, consumed by IOutboxRelayService
/// (Core.Application). Implemented in Core.Persistence directly against ApexBookingDbContext —
/// OutboxMessage is not an IAggregateRoot, so it has no IGenericRepository (same reasoning as
/// SmsUsage/ISmsQuotaService).
/// </summary>
public interface IOutboxStore
{
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default);

    // Atomically flips a single row from Pending -> Processing (conditional update, same technique
    // as ISmsQuotaService.TryConsumeAsync). Returns null if the row doesn't exist, isn't Pending, or
    // another caller already claimed it first — the immediate trigger and the recurring sweep can
    // both reach for the same id, and only one may win.
    Task<OutboxMessage?> TryClaimAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);

    // isTransient=false skips the message's retry budget and fails it permanently right away — see
    // OutboxMessage.MarkFailedPermanently and EmailDeliveryException.IsTransient.
    Task MarkFailedAsync(Guid id, string error, bool isTransient = true, CancellationToken cancellationToken = default);

    // SuperAdmin failed-jobs view.
    Task<IReadOnlyList<OutboxMessage>> GetFailedAsync(CancellationToken cancellationToken = default);

    // Flips a Failed row back to Pending with a fresh retry budget. Returns false if the row
    // doesn't exist or isn't currently Failed (e.g. it already got retried by someone else, or the
    // recurring sweep already picked it up before this click landed).
    Task<bool> RetryAsync(Guid id, CancellationToken cancellationToken = default);
}
