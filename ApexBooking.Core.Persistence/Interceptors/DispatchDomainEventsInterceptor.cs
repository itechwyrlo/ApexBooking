using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Persistence.Interceptors
{
    public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
    {
        private readonly IDomainEventDispatcher _dispatcher;

        public DispatchDomainEventsInterceptor(IDomainEventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
            {
                await GatherAndDispatchEventsAsync(eventData.Context);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private async Task GatherAndDispatchEventsAsync(DbContext context)
        {
            // Change Tracker scans for any entity using our SharedKernel interface
            var entitiesWithEvents = context.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Where(x => x.Entity.DomainEvents.Any())
                .Select(x => x.Entity)
                .ToList();

            await _dispatcher.DispatchAndClearAsync(entitiesWithEvents);
        }
    }
}