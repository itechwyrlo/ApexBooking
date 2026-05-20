using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface ISuperAdminRepository : IGenericRepository<SuperAdmin>
{
    //unused method
    Task<SuperAdmin?> FindByEmailAsync(string email);
    //unused method
    Task<bool> EmailExistsAsync(string email);
    Task<SuperAdmin?> FindByRefreshTokenAsync(string refreshToken);
}
