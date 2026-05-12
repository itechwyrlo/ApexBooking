using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories;

public class PaymentTransactionRepository : GenericRepository<PaymentTransaction>, IPaymentTransactionRepository
{
    public PaymentTransactionRepository(ApexBookingDbContext context) : base(context) { }

    //unused method
    public async Task<PaymentTransaction?> GetByBookingIdAsync(
        BookingId bookingId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentTransaction>()
            .FirstOrDefaultAsync(pt => pt.BookingId == bookingId, cancellationToken);
    }
    
     //unused method
    public async Task<PaymentTransaction?> GetByGatewayTransactionIdAsync(
        string gatewayTransactionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<PaymentTransaction>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(pt => pt.GatewayTransactionId == gatewayTransactionId, cancellationToken);
    }
}