using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.WeeklySchedule
{
    public class UpdateWeeklyScheduleCommandHandler : ICommandHandler<UpdateWeeklyScheduleCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public UpdateWeeklyScheduleCommandHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task Handle(UpdateWeeklyScheduleCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to update schedule. No authenticated tenant context was found.");

            var currentTenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Members
            );

            if (currentTenant == null)
                throw new BusinessRuleBrokenException("Failed to update schedule. Isolated business workspace could not be verified.");

            var member = currentTenant.Members.FirstOrDefault(m => m.TenantMemberId.Value == command.TenantMemberId);
            if (member == null)
                throw new BusinessRuleBrokenException("Target team member record not found within this business workspace.");

            foreach (var item in command.Schedules)
            {
                member.SetDaySchedule(
                    item.DayOfWeek,
                    item.StartTime,
                    item.EndTime,
                    item.IsOff
                );
            }
            _unitOfWork.TenantRepository.Update(currentTenant);

            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}