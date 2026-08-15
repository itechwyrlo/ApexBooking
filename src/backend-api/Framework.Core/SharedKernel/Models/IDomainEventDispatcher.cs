namespace ApexBooking.SharedKernel.Models
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAndClearAsync(IEnumerable<IHasDomainEvents> entitiesWithEvents);
    }
}