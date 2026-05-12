using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface IPlatformPaymentGatewayRepository : IGenericRepository<PlatformPaymentGateway>
{
    Task<PlatformPaymentGateway?> GetActiveAsync(CancellationToken cancellationToken = default);
}
