using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface ISuperAdminRepository : IGenericRepository<SuperAdmin>
{
    Task<SuperAdmin?> FindByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
}
