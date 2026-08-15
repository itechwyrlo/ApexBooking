using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.TimeOffs.Commands.ApproveTimeOff
{
    public class ApproveTimeOffHandler : ICommandHandler<ApproveTimeOffCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public ApproveTimeOffHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(ApproveTimeOffCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to approve time-off request. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to approve time-off request. Isolated tenant context could not be verified.");

            var requestId = new StaffTimeOffRequestId(command.TimeOffRequestId);
            var member = tenant.Members.FirstOrDefault(m => m.TimeOffRequests.Any(r => r.Id == requestId))
                ?? throw new BusinessRuleBrokenException("Target time-off request was not found within this business workspace.");

            member.ApproveTimeOff(requestId);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}
