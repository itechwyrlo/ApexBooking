using ApexBooking.Core.Application.Interfaces;
using ApexBooking.Core.Application.mapper;
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
        private readonly IPushNotificationService _pushService;
        private readonly IRealtimeNotificationService _realtimeService;

        public DeactivateStaffCommandHandler(
            IUnitOfWork unitOfWork,
            IUserContextService contextService,
            IPushNotificationService pushService,
            IRealtimeNotificationService realtimeService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
            _pushService = pushService;
            _realtimeService = realtimeService;
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
            var adminNotifications = new List<Notification>();
            foreach (var admin in admins)
            {
                var n = Notification.Create(
                    admin.Id,
                    NotificationRecipientType.TenantAdmin,
                    tenantId,
                    NotificationEventType.StaffDeactivated,
                    "Staff Deactivated",
                    $"Staff record for {staff.FirstName} {staff.LastName} has been deactivated.");
                _unitOfWork.NotificationRepository.Add(n);
                adminNotifications.Add(n);
            }

            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            await _pushService.SendAsync(
                adminNotifications.Select(n => n.RecipientId).ToList(),
                "Staff Deactivated",
                $"Staff record for {staff.FirstName} {staff.LastName} has been deactivated.",
                NotificationEventType.StaffDeactivated.ToString(),
                cancellationToken);
            await Task.WhenAll(adminNotifications.Select(n => _realtimeService.SendAsync(n.RecipientId, n.ToDto())));
        }
    }
}