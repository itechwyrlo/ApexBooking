using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.BookableStaffs
{
    public class GetBookableStaffHandler : IQueryHandler<GetBookableStaffQuery, IReadOnlyCollection<BookableStaffSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetBookableStaffHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<BookableStaffSummary>> Handle(GetBookableStaffQuery query, CancellationToken cancellationToken)
        {
            // Single database load of the Tenant aggregate root hydrating both cross-cutting sub-collections.
            // Tenant does not implement ITenantEntity, so the global EF query filter doesn't cover this
            // lookup — scope explicitly via the ambient ITenantEntity.
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Workspace scope could not be verified.");

            // Uses GetWithServiceStaffAsync (not the generic GetAsync) because Service.ServiceProviders
            // needs a ThenInclude, which the generic repository's single-level Include() can't express.
            var tenant = await _unitOfWork.TenantRepository.GetWithServiceStaffAsync(tenantId, cancellationToken);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Workspace scope could not be verified.");

            var branch = tenant.Branches.FirstOrDefault(b => b.BranchId.Value == query.BranchId && b.IsActive)
                ?? throw new BusinessRuleBrokenException("The selected branch is unavailable.");

            var targetService = tenant.Services.FirstOrDefault(s => s.ServiceId.Value == query.ServiceId);
            if (targetService is null || !targetService.IsActive)
                throw new BusinessRuleBrokenException("The requested service catalog item is unavailable.");

            // Filter the parent's Team Members array by cross-referencing qualification matches
            // AND an explicit branch-deployment check — only staff deployed to the chosen branch qualify.
            var qualifiedStaff = tenant.Members
                .Where(member => member.IsActive &&
                                 member.BranchId == branch.BranchId &&
                                 targetService.ServiceProviders.Any(prov => prov.TenantMemberId == member.TenantMemberId))
                .OrderBy(m => m.FirstName)
                .Select(m => new BookableStaffSummary(
                    TenantMemberId: m.TenantMemberId.Value,
                    FullName: $"{m.FirstName} {m.LastName}".Trim(),
                    CustomJobTitle: m.CustomJobTitle,
                    PhotoUrl: m.PhotoUrl
                ))
                .ToList();

            return qualifiedStaff;
        }
    }
}
