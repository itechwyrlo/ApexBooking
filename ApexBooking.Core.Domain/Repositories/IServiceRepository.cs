using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories
{
    public interface IServiceRepository : IGenericRepository<Service>
    {
        //unused method
        Task<Service?> FindByNameAsync(string name);
        Task<bool> NameExistsAsync(string name);

    }
}