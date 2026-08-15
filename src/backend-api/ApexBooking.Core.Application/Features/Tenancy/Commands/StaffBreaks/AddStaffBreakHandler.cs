using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.StaffBreaks
{
    public class AddStaffBreakHandler : ICommandHandler<AddStaffBreakCommand, Guid>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public AddStaffBreakHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<Guid> Handle(AddStaffBreakCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to add break. No authenticated tenant context was found.");

            var currentTenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members
            );

            if (currentTenant == null)
                throw new BusinessRuleBrokenException("Failed to add break. Isolated business workspace could not be verified.");

            var member = currentTenant.Members.FirstOrDefault(m => m.TenantMemberId.Value == command.TenantMemberId);
            if (member == null)
                throw new BusinessRuleBrokenException("Target team member record not found within this business workspace.");

            var isOverlapping = member.Breaks.Any(b => b.OverlapsWith(command.StartTime, command.EndTime));
            if (isOverlapping)
                throw new BusinessRuleBrokenException("Failed to add break. The requested interval conflicts with an already configured staff break slot.");

            var newBreak = member.AddBreak(
                command.Name,
                command.StartTime,
                command.EndTime
            );

            _unitOfWork.TenantRepository.Update(currentTenant);

            await _unitOfWork.CompleteAsync(cancellationToken);

            return newBreak.Id.Value;
        }
    }
}