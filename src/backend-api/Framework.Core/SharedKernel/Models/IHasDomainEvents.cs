namespace ApexBooking.SharedKernel.Models
{
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();

        // Removes a single event, e.g. after it has been extracted into an outbox row so it isn't
        // also re-dispatched through the immediate post-commit path (see UnitOfWork.CompleteAsync).
        void RemoveDomainEvent(IDomainEvent domainEvent);
    }
}