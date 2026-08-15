using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.TimeOffs.Commands.BlockMyTime
{
    public class BlockMyTimeCommandHandler : ICommandHandler<BlockMyTimeCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITenantEntity _tenantEntity;

        public BlockMyTimeCommandHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tenantEntity = tenantEntity;
        }

        public async Task<Guid> Handle(BlockMyTimeCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to block your time. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to block your time. Isolated tenant context could not be verified.");

            var currentUserId = _userContext.GetCurrentUserId();
            var currentMember = tenant.Members.FirstOrDefault(m => m.UserId == currentUserId && m.IsActive)
                ?? throw new BusinessRuleBrokenException("Your account is not linked to an active staff account.");

            var request = currentMember.RequestTimeOff(
                TimeOffType.PartialDay,
                command.Date,
                command.Date,
                command.StartTime,
                command.EndTime,
                command.Reason);

            // Instant self-approval — the only difference from the normal RequestTimeOff flow.
            currentMember.ApproveTimeOff(request.Id);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return request.Id.Value;
        }
    }
}
