using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface IFcmTokenRepository : IGenericRepository<FcmToken>
{
    Task<IReadOnlyList<string>> GetTokensByUserIdAsync(Guid userId, CancellationToken ct);
    Task<FcmToken?> GetByUserAndTokenAsync(Guid userId, string token, CancellationToken ct);
    Task DeleteByUserIdAsync(Guid userId, string token, CancellationToken ct);
}
