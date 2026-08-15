using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetTeamMemberRemovalImpact
{
    public class GetTeamMemberRemovalImpactHandler : IQueryHandler<GetTeamMemberRemovalImpactQuery, TeamMemberRemovalImpact>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetTeamMemberRemovalImpactHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<TeamMemberRemovalImpact> Handle(GetTeamMemberRemovalImpactQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to check team member. No authenticated tenant context was found.");

            var hasBookings = await _unitOfWork.TenantRepository.StaffHasBookingsAsync(
                tenantId,
                new TenantMemberId(query.TenantMemberId),
                cancellationToken);

            return new TeamMemberRemovalImpact(hasBookings);
        }
    }
}
