using MediatR;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Common.DomainEvent
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IPublisher _publisher;

        public DomainEventDispatcher(IPublisher publisher) => _publisher = publisher;

        public async Task DispatchAndClearAsync(IEnumerable<IHasDomainEvents> entitiesWithEvents)
        {
            foreach (var entity in entitiesWithEvents)
            {
              
                var domainEvents = entity.DomainEvents.ToArray();
                entity.ClearDomainEvents();

                foreach (var domainEvent in domainEvents)
                {
                    var eventType = domainEvent.GetType();

                    // Construct the wrapper dynamically
                    var notificationType = typeof(DomainEventNotification<>)
                        .MakeGenericType(eventType);

                    var notification = Activator.CreateInstance(notificationType, domainEvent);

                    if (notification is INotification mediatrNotification)
                    {
                        await _publisher.Publish(mediatrNotification);
                    }
                }
            }
        }
    }
}