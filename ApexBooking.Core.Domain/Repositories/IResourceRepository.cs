using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories
{
    /// <summary>
    /// Repository for the Resource aggregate root only.
    /// Children (ResourceAvailabilitySchedule, ResourceBreakPeriod,
    /// ResourceAvailabilityException) are loaded as part of the root's
    /// navigation properties — never queried independently.
    /// </summary>
    public interface IResourceRepository : IGenericRepository<Resource>
    {
        /// <summary>
        /// Loads the Resource with its full availability child graph:
        ///   AvailabilitySchedules → BreakPeriods
        ///   AvailabilityExceptions
        ///
        /// The slot calculator needs all of these in one load to avoid
        /// N+1 queries. Children are accessed through the root's
        /// IReadOnlyCollection navigation properties.
        /// TR-9.1 Steps 4, 5, 6, 7
        /// </summary>
        Task<Resource?> FindByIdWithAvailabilityAsync(
            ResourceId resourceId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Resource>> FindByIdsWithAvailabilityAsync(
            IEnumerable<ResourceId> resourceIds,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<Resource>> GetByTenantWithAvailabilityAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default);
    }
}