using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.DeactivateStaff
{
    internal sealed class DeactivateStaffCommandHandler : ICommandHandler<DeactivateStaffCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;

        public DeactivateStaffCommandHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
        }

        public async Task Handle(DeactivateStaffCommand command, CancellationToken cancellationToken)
        {
            var tenantId = _contextService.GetCurrentTenantId();
            var staffId = new StaffId(command.staffId);

            var staff = await _unitOfWork.StaffRepository
                .GetByIdAsync(staffId)
                .ConfigureAwait(false);

            if (staff is null || staff.TenantId != tenantId)
                throw new NotFoundException("Staff not found.");

            staff.Deactivate();

            _unitOfWork.StaffRepository.Update(staff);

            var admins = await _unitOfWork.UserRepository.GetUsersByRoleAsync(tenantId, UserRole.TenantAdmin);
            foreach (var admin in admins)
            {
                _unitOfWork.NotificationRepository.Add(Notification.Create(
                    admin.Id,
                    NotificationRecipientType.TenantAdmin,
                    tenantId,
                    NotificationEventType.StaffDeactivated,
                    "Staff Deactivated",
                    $"Staff record for {staff.FirstName} {staff.LastName} has been deactivated."));
            }

            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}