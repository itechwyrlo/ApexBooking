using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface IGuestCancellationTokenRepository : IGenericRepository<GuestCancellationToken>
{
    Task<GuestCancellationToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
