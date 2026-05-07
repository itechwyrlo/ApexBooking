using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories
{
    public interface IServiceRepository : IGenericRepository<Service>
    {
        Task<Service?> FindByNameAsync(string name);
        Task<bool> NameExistsAsync(string name);
        Task<List<Service>> GetActiveServicesAsync();
        Task<List<Service>> GetActiveServicesByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);
        Task<List<Service>> GetServicesByResourceAsync(ResourceId resourceId);
        Task<Service?> GetServiceWithResourcesAsync(ServiceId serviceId);
        Task<Service?> FindByIdWithResourcesAsync(ServiceId serviceId, CancellationToken cancellationToken = default);
    }
}