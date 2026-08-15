using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Application.Common.Outbox;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Outbox;

public class OutboxRelayServiceTests
{
    private sealed class FakeOutboxStore : IOutboxStore
    {
        private readonly Dictionary<Guid, OutboxMessage> _messages = new();
        private readonly HashSet<Guid> _alreadyClaimed = new();

        public List<Guid> Processed { get; } = new();
        public List<(Guid Id, string Error)> Failed { get; } = new();

        public void Seed(OutboxMessage message) => _messages[message.Id] = message;
        public void SimulateAlreadyClaimedBySomeoneElse(Guid id) => _alreadyClaimed.Add(id);

        public Task<IReadOnlyList<Guid>> GetPendingIdsAsync(int batchSize, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Guid>>(_messages.Keys.Take(batchSize).ToList());

        public Task<OutboxMessage?> TryClaimAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_alreadyClaimed.Contains(id) || !_messages.TryGetValue(id, out var message))
                return Task.FromResult<OutboxMessage?>(null);

            return Task.FromResult<OutboxMessage?>(message);
        }

        public Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Processed.Add(id);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
        {
            Failed.Add((id, error));
            return Task.CompletedTask;
        }

        // Added to satisfy IOutboxStore's SuperAdmin failed-jobs surface (GetFailedAsync/RetryAsync)
        // — not exercised by any test in this file yet, so kept as a minimal, faithful mirror of the
        // real OutboxStore's semantics rather than a no-op: GetFailedAsync reports whatever
        // MarkFailedAsync has recorded, and RetryAsync only succeeds for ids currently in that list.
        public Task<IReadOnlyList<OutboxMessage>> GetFailedAsync(CancellationToken cancellationToken = default)
        {
            var failedIds = Failed.Select(f => f.Id).ToHashSet();
            IReadOnlyList<OutboxMessage> result = _messages.Values.Where(m => failedIds.Contains(m.Id)).ToList();
            return Task.FromResult(result);
        }

        public Task<bool> RetryAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var wasFailed = Failed.RemoveAll(f => f.Id == id) > 0;
            return Task.FromResult(wasFailed);
        }
    }

    private sealed class SpyPublisher : IPublisher
    {
        public List<object> Published { get; } = new();
        public bool ThrowOnPublish { get; set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            if (ThrowOnPublish)
                throw new InvalidOperationException("boom");

            Published.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (ThrowOnPublish)
                throw new InvalidOperationException("boom");

            Published.Add(notification!);
            return Task.CompletedTask;
        }
    }

    // A real IReliableDomainEvent from Core.Domain.Events, not a test-local type — the real
    // OutboxRelayService resolves EventType via Type.GetType($"{EventType}, ApexBooking.Core.Domain"),
    // so the round-trip only works for types that actually live in that assembly.
    private static (OutboxMessage Message, TenantCreatedDomainEvent Event) SeedTenantCreatedMessage(FakeOutboxStore store)
    {
        var domainEvent = new TenantCreatedDomainEvent(
            new TenantId(Guid.NewGuid()), "acme-salon", "Acme Salon", "Ada", "Lovelace", "ada@example.com", DateTime.UtcNow);

        var message = OutboxMessage.Create(
            domainEvent.GetType().FullName!,
            System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            DateTime.UtcNow);

        store.Seed(message);
        return (message, domainEvent);
    }

    [Fact]
    public async Task ReplayAsync_deserializes_publishes_and_marks_processed()
    {
        var store = new FakeOutboxStore();
        var publisher = new SpyPublisher();
        var relay = new OutboxRelayService(store, publisher, NullLogger<OutboxRelayService>.Instance);
        var (message, domainEvent) = SeedTenantCreatedMessage(store);

        await relay.ReplayAsync(new[] { message.Id });

        var published = Assert.Single(publisher.Published);
        var notification = Assert.IsType<DomainEventNotification<TenantCreatedDomainEvent>>(published);
        Assert.Equal(domainEvent, notification.DomainEvent);
        Assert.Equal(message.Id, Assert.Single(store.Processed));
        Assert.Empty(store.Failed);
    }

    [Fact]
    public async Task ReplayPendingAsync_processes_everything_the_store_reports_pending()
    {
        var store = new FakeOutboxStore();
        var publisher = new SpyPublisher();
        var relay = new OutboxRelayService(store, publisher, NullLogger<OutboxRelayService>.Instance);
        SeedTenantCreatedMessage(store);
        SeedTenantCreatedMessage(store);

        await relay.ReplayPendingAsync(batchSize: 10);

        Assert.Equal(2, publisher.Published.Count);
        Assert.Equal(2, store.Processed.Count);
    }

    [Fact]
    public async Task A_failed_publish_marks_the_message_failed_instead_of_throwing()
    {
        var store = new FakeOutboxStore();
        var publisher = new SpyPublisher { ThrowOnPublish = true };
        var relay = new OutboxRelayService(store, publisher, NullLogger<OutboxRelayService>.Instance);
        var (message, _) = SeedTenantCreatedMessage(store);

        // Must not throw — one bad message can never fail the caller's batch loop.
        await relay.ReplayAsync(new[] { message.Id });

        Assert.Empty(store.Processed);
        var failure = Assert.Single(store.Failed);
        Assert.Equal(message.Id, failure.Id);
        Assert.Contains("boom", failure.Error);
    }

    [Fact]
    public async Task A_message_already_claimed_by_the_other_entry_point_is_silently_skipped()
    {
        var store = new FakeOutboxStore();
        var publisher = new SpyPublisher();
        var relay = new OutboxRelayService(store, publisher, NullLogger<OutboxRelayService>.Instance);
        var (message, _) = SeedTenantCreatedMessage(store);
        store.SimulateAlreadyClaimedBySomeoneElse(message.Id);

        await relay.ReplayAsync(new[] { message.Id });

        Assert.Empty(publisher.Published);
        Assert.Empty(store.Processed);
        Assert.Empty(store.Failed); // not a failure — just lost the race, nothing to report
    }

    [Fact]
    public async Task One_bad_message_does_not_stop_the_rest_of_the_batch()
    {
        var store = new FakeOutboxStore();
        var publisher = new SpyPublisher();
        var relay = new OutboxRelayService(store, publisher, NullLogger<OutboxRelayService>.Instance);
        var (goodMessage, _) = SeedTenantCreatedMessage(store);

        // A message with an unparseable payload — should fail in isolation, not blow up the batch.
        var corrupted = OutboxMessage.Create(typeof(TenantCreatedDomainEvent).FullName!, "{ not valid json", DateTime.UtcNow);
        store.Seed(corrupted);

        await relay.ReplayAsync(new[] { corrupted.Id, goodMessage.Id });

        Assert.Single(store.Failed);
        Assert.Equal(corrupted.Id, store.Failed[0].Id);
        Assert.Single(store.Processed);
        Assert.Equal(goodMessage.Id, store.Processed[0]);
    }
}
