using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories
{
    public interface IStaffRepository : IGenericRepository<Staff>
    {
        Task<Staff?> FindByIdWithAvailabilityAsync(
            StaffId StaffId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Staff>> FindByIdsWithAvailabilityAsync(
            IEnumerable<StaffId> StaffIds,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Staff>> GetByTenantWithAvailabilityAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default);

        Task<Staff?> FindByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}