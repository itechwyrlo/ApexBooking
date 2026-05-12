using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface IPaymentTransactionRepository : IGenericRepository<PaymentTransaction>
{
    //unused method
    Task<PaymentTransaction?> GetByBookingIdAsync(BookingId bookingId, CancellationToken cancellationToken = default);
     //unused method
    Task<PaymentTransaction?> GetByGatewayTransactionIdAsync(string gatewayTransactionId, CancellationToken cancellationToken = default);
}