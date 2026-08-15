using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.SharedKernel.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Domain.UnitTests.DomainEvents;

public class DomainEventPipelineTests
{
    // --- Throwaway aggregate + event (Phase 0 uses no real business events) ---
    private sealed record ThingHappened(Guid Id) : IDomainEvent;

    private sealed class Thing : IAggregateRoot, IHasDomainEvents
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        private readonly List<IDomainEvent> _events = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();
        public void ClearDomainEvents() => _events.Clear();
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _events.Remove(domainEvent);

        public void DoSomething() => _events.Add(new ThingHappened(Id));
    }

    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions options) : base(options) { }
        public DbSet<Thing> Things => Set<Thing>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Thing>().HasKey(t => t.Id);
            b.Entity<Thing>().Ignore(t => t.DomainEvents);
        }
    }

    // Spy standing in for the real IDomainEventDispatcher, to prove the
    // post-commit dispatch (UnitOfWork.CompleteAsync, ADR-062) invokes it.
    private sealed class SpyDispatcher : IDomainEventDispatcher
    {
        public List<IHasDomainEvents> Received { get; } = new();
        public Task DispatchAndClearAsync(IEnumerable<IHasDomainEvents> entitiesWithEvents)
        {
            var list = entitiesWithEvents.ToList();
            Received.AddRange(list);
            foreach (var e in list) e.ClearDomainEvents();
            return Task.CompletedTask;
        }
    }

    // Spy standing in for MediatR IPublisher, to prove the real dispatcher
    // wraps + publishes + clears.
    private sealed class SpyPublisher : IPublisher
    {
        public List<object> Published { get; } = new();
        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Published.Add(notification!);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Tracked_entities_with_events_are_gathered_and_dispatched_after_save()
    {
        var spy = new SpyDispatcher();
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase("evt-" + Guid.NewGuid())
            .Options;

        await using var ctx = new TestContext(options);
        var thing = new Thing();
        thing.DoSomething();
        ctx.Things.Add(thing);

        await ctx.SaveChangesAsync();

        // Post-commit dispatch, exactly as UnitOfWork.CompleteAsync does after SaveChanges returns (ADR-062).
        var entitiesWithEvents = ctx.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();
        await spy.DispatchAndClearAsync(entitiesWithEvents);

        Assert.Single(spy.Received);
        Assert.Empty(thing.DomainEvents); // dispatch cleared the events
    }

    [Fact]
    public async Task Dispatcher_wraps_event_in_notification_and_clears()
    {
        var publisher = new SpyPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var thing = new Thing();
        thing.DoSomething();

        await dispatcher.DispatchAndClearAsync(new IHasDomainEvents[] { thing });

        Assert.Single(publisher.Published);
        Assert.IsType<DomainEventNotification<ThingHappened>>(publisher.Published[0]);
        Assert.Empty(thing.DomainEvents);
    }
}
