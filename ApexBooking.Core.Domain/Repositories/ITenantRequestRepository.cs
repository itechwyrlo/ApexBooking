using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface ITenantRequestRepository : IGenericRepository<TenantRequest>
{
    Task<IEnumerable<TenantRequest>> GetPendingAsync(CancellationToken cancellationToken = default);
}
