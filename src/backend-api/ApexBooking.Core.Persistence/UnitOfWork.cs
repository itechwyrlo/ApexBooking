using System.Text.Json;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.Core.Persistence.Identity;
using ApexBooking.Core.Persistence.Repositories;
using ApexBooking.SharedKernel.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Persistence
{
    public class UnitOfWork : IUnitOfWork, IAsyncDisposable
    {
        private readonly ApexBookingDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDomainEventDispatcher _domainEventDispatcher;
        private readonly IOutboxTrigger _outboxTrigger;
        private readonly ILogger<UnitOfWork> _logger;

        // Lazy holders for our repositories
        private readonly Lazy<ITenantRepository> _tenantRepository;
        private readonly Lazy<ITenantRegistrationRequestRepository> _tenantRegistrationRequestRepository;
        private readonly Lazy<ICustomerRepository> _customerRepository;

        private readonly Lazy<INotificationRepository> _notificationRepository;
        private readonly Lazy<IFcmTokenRepository> _fcmTokenRepository;
        public ITenantRepository TenantRepository => _tenantRepository.Value;
        public ITenantRegistrationRequestRepository TenantRegistrationRequestRepository => _tenantRegistrationRequestRepository.Value;
        public ICustomerRepository CustomerRepository => _customerRepository.Value;
        public INotificationRepository NotificationRepository => _notificationRepository.Value;
        public IFcmTokenRepository FcmTokenRepository => _fcmTokenRepository.Value;

        public UserManager<ApplicationUser> UserManager { get; }
        public RoleManager<IdentityRole<Guid>> RoleManager { get; }

        public UnitOfWork(
            ApexBookingDbContext context,
            IServiceProvider serviceProvider,
            IDomainEventDispatcher domainEventDispatcher,
            IOutboxTrigger outboxTrigger,
            ILogger<UnitOfWork> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
            _outboxTrigger = outboxTrigger ?? throw new ArgumentNullException(nameof(outboxTrigger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            RoleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));

            _tenantRepository = new Lazy<ITenantRepository>(() => new TenantRepository(_context));
            _tenantRegistrationRequestRepository = new Lazy<ITenantRegistrationRequestRepository>(() => new TenantRegistrationRequestRepository(_context));
            _customerRepository = new Lazy<ICustomerRepository>(() => new CustomerRepository(_context));
            _notificationRepository = new Lazy<INotificationRepository>(
                () => new NotificationRepository(_context));
            _fcmTokenRepository = new Lazy<IFcmTokenRepository>(
                () => new FcmTokenRepository(_context));
        }

        public Task<int> CompleteAsync() => CompleteAsync(CancellationToken.None);

        public async Task<int> CompleteAsync(CancellationToken cancellationToken)
        {
            var entitiesWithEvents = _context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            // Must run before SaveChangesAsync: the whole point of the outbox is that the
            // OutboxMessage row commits in the SAME transaction as the business write it
            // describes (see OutboxMessage's doc comment).
            var newOutboxMessageIds = ExtractReliableEventsToOutbox(entitiesWithEvents);

            var result = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Reliable events were already pulled out above, so this only ever dispatches the
            // remaining non-reliable events (e.g. ProvisionTenantOnRequestApprovedHandler) —
            // unchanged synchronous, in-request behavior.
            if (entitiesWithEvents.Any(e => e.DomainEvents.Count > 0))
                await _domainEventDispatcher.DispatchAndClearAsync(entitiesWithEvents).ConfigureAwait(false);

            if (newOutboxMessageIds.Count > 0)
                await NotifyOutboxTriggerAsync(newOutboxMessageIds, cancellationToken).ConfigureAwait(false);

            return result;
        }

        private IReadOnlyList<Guid> ExtractReliableEventsToOutbox(IReadOnlyList<IHasDomainEvents> entitiesWithEvents)
        {
            var newOutboxMessageIds = new List<Guid>();

            foreach (var entity in entitiesWithEvents)
            {
                foreach (var domainEvent in entity.DomainEvents.OfType<IReliableDomainEvent>().ToArray())
                {
                    var outboxMessage = OutboxMessage.Create(
                        domainEvent.GetType().FullName!,
                        JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                        DateTime.UtcNow);

                    _context.Add(outboxMessage);
                    entity.RemoveDomainEvent(domainEvent);
                    newOutboxMessageIds.Add(outboxMessage.Id);
                }
            }

            return newOutboxMessageIds;
        }

        private async Task NotifyOutboxTriggerAsync(IReadOnlyList<Guid> outboxMessageIds, CancellationToken cancellationToken)
        {
            try
            {
                await _outboxTrigger.NotifyAsync(outboxMessageIds, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Non-fatal by design (see IOutboxTrigger's doc comment): the business transaction,
                // and the outbox row itself, already committed successfully. Worst case here is
                // falling back to the recurring sweep's poll-interval latency for these messages.
                _logger.LogWarning(
                    ex,
                    "IOutboxTrigger.NotifyAsync failed for {Count} outbox message(s); falling back to the recurring sweep.",
                    outboxMessageIds.Count);
            }
        }

        public ValueTask DisposeAsync()
        {
            return _context.DisposeAsync();
        }
    }
}
