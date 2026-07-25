using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.SharedKernel.Models
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAndClearAsync(IEnumerable<IHasDomainEvents> entitiesWithEvents);
    }
}