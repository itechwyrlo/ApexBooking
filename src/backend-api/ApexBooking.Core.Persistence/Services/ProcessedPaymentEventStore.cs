using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Services;

public sealed class ProcessedPaymentEventStore : IProcessedPaymentEventStore
{
    private readonly ApexBookingDbContext _context;

    public ProcessedPaymentEventStore(ApexBookingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string payMongoEventId, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessedPaymentEvents
            .AnyAsync(e => e.PayMongoEventId == payMongoEventId, cancellationToken);
    }

    public void Add(ProcessedPaymentEvent processedPaymentEvent)
    {
        _context.ProcessedPaymentEvents.Add(processedPaymentEvent);
    }
}
