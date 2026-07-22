using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories;

public interface ITenantRepository : IGenericRepository<Tenant>
{
    Task<Tenant?> FindBySlugAsync(string slug);
    Task<Tenant?> FindByEmailAsync(string email);
    //unused method
    Task<bool> SlugExistsAsync(string slug);
    //unused method
    Task<bool> EmailExistsAsync(string email);

}