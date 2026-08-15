using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.RemoveTeamMember
{
    public class RemoveTeamMemberHandler : ICommandHandler<RemoveTeamMemberCommand, TeamMemberRemovalResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public RemoveTeamMemberHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<TeamMemberRemovalResult> Handle(RemoveTeamMemberCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to remove team member. No authenticated tenant context was found.");

            var memberId = new TenantMemberId(command.TenantMemberId);

            // Re-verified here server-side rather than trusted from an earlier client-side check —
            // the same posture as every other "preview then act" flow in this codebase.
            var hasBookings = await _unitOfWork.TenantRepository.StaffHasBookingsAsync(tenantId, memberId, cancellationToken);

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to remove team member. Isolated tenant context could not be verified.");

            if (hasBookings)
            {
                tenant.DeactivateMember(memberId);
            }
            else
            {
                tenant.RemoveMember(memberId);
            }

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new TeamMemberRemovalResult(WasSoftDeleted: hasBookings);
        }
    }
}
