using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Events;
using ApexBooking.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Domain.UnitTests.DomainEvents;

/// <summary>
/// Mirrors UnitOfWork.ExtractReliableEventsToOutbox / CompleteAsync's split, the same way
/// DomainEventPipelineTests mirrors the post-commit dispatch — a throwaway DbContext/aggregate
/// rather than standing up the real ApexBookingDbContext/UnitOfWork (which needs
/// UserManager/RoleManager scaffolding unrelated to this behavior).
/// </summary>
public class OutboxExtractionTests
{
    private sealed record ThingHappenedReliably(Guid Id) : IReliableDomainEvent;
    private sealed record ThingLogged(Guid Id) : IDomainEvent;

    private sealed class Thing : IAggregateRoot, IHasDomainEvents
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        private readonly List<IDomainEvent> _events = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();
        public void ClearDomainEvents() => _events.Clear();
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _events.Remove(domainEvent);

        public void DoSomethingReliable() => _events.Add(new ThingHappenedReliably(Id));
        public void DoSomethingLoggable() => _events.Add(new ThingLogged(Id));
    }

    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions options) : base(options) { }
        public DbSet<Thing> Things => Set<Thing>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Thing>().HasKey(t => t.Id);
            b.Entity<Thing>().Ignore(t => t.DomainEvents);
            b.Entity<OutboxMessage>().HasKey(m => m.Id);
        }
    }

    // The same extraction algorithm UnitOfWork.ExtractReliableEventsToOutbox runs — see that
    // method's own doc comment for why it must run before SaveChangesAsync.
    private static List<Guid> ExtractReliableEventsToOutbox(TestContext ctx, IReadOnlyList<IHasDomainEvents> entitiesWithEvents)
    {
        var newOutboxMessageIds = new List<Guid>();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents.OfType<IReliableDomainEvent>().ToArray())
            {
                var outboxMessage = OutboxMessage.Create(
                    domainEvent.GetType().FullName!,
                    System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    DateTime.UtcNow);

                ctx.Add(outboxMessage);
                entity.RemoveDomainEvent(domainEvent);
                newOutboxMessageIds.Add(outboxMessage.Id);
            }
        }

        return newOutboxMessageIds;
    }

    [Fact]
    public async Task Reliable_event_becomes_an_outbox_row_and_is_removed_from_the_entity()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase("outbox-" + Guid.NewGuid())
            .Options;

        await using var ctx = new TestContext(options);
        var thing = new Thing();
        thing.DoSomethingReliable();
        ctx.Things.Add(thing);

        var entitiesWithEvents = ctx.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var newIds = ExtractReliableEventsToOutbox(ctx, entitiesWithEvents);
        await ctx.SaveChangesAsync();

        Assert.Single(newIds);
        Assert.Empty(thing.DomainEvents); // extracted, not left for post-commit dispatch
        Assert.Single(ctx.OutboxMessages);
        var stored = ctx.OutboxMessages.Single();
        Assert.Equal("ApexBooking.Core.Domain.UnitTests.DomainEvents.OutboxExtractionTests+ThingHappenedReliably", stored.EventType);
    }

    [Fact]
    public async Task Non_reliable_event_is_left_alone_for_the_existing_post_commit_dispatch()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase("outbox-" + Guid.NewGuid())
            .Options;

        await using var ctx = new TestContext(options);
        var thing = new Thing();
        thing.DoSomethingLoggable();
        ctx.Things.Add(thing);

        var entitiesWithEvents = ctx.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var newIds = ExtractReliableEventsToOutbox(ctx, entitiesWithEvents);
        await ctx.SaveChangesAsync();

        Assert.Empty(newIds);
        Assert.Empty(ctx.OutboxMessages); // no outbox row for a non-reliable event
        Assert.Single(thing.DomainEvents); // untouched — still there for the post-commit dispatcher
    }

    [Fact]
    public async Task Mixed_events_split_correctly_between_outbox_and_immediate_dispatch()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase("outbox-" + Guid.NewGuid())
            .Options;

        await using var ctx = new TestContext(options);
        var thing = new Thing();
        thing.DoSomethingReliable();
        thing.DoSomethingLoggable();
        ctx.Things.Add(thing);

        var entitiesWithEvents = ctx.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var newIds = ExtractReliableEventsToOutbox(ctx, entitiesWithEvents);
        await ctx.SaveChangesAsync();

        Assert.Single(newIds);
        Assert.Single(ctx.OutboxMessages);
        Assert.Single(thing.DomainEvents); // only the non-reliable one remains
        Assert.IsType<ThingLogged>(thing.DomainEvents.Single());
    }
}
