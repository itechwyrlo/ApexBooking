using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;

namespace ApexBooking.Core.Domain.Repositories
{
    public interface IServiceRepository : IGenericRepository<Service>
    {
        //unused method
        Task<Service?> FindByNameAsync(string name);
        Task<bool> NameExistsAsync(string name);

    }
}