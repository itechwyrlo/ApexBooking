using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.CreateStaff
{
    internal sealed class CreateStaffHandler : ICommandHandler<CreateStaffCommand, StaffDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _contextService;
        private readonly IAuthNotificationService _authNotification;
        private readonly IAppUrlService _appUrlService;

        public CreateStaffHandler(
            IUnitOfWork unitOfWork,
            IUserContextService contextService,
            IAuthNotificationService authNotification,
            IAppUrlService appUrlService)
        {
            _unitOfWork = unitOfWork;
            _contextService = contextService;
            _authNotification = authNotification;
            _appUrlService = appUrlService;
        }

        public async Task<StaffDto> Handle(CreateStaffCommand command, CancellationToken ct)
        {
            var tenantId = _contextService.GetCurrentTenantId();

            var tenant = await _unitOfWork.TenantRepository.GetAsync(t => t.TenantId == tenantId);
            if (tenant is null)
                throw new NotFoundException("Tenant not found.");

            var existing = await _unitOfWork.StaffRepository.GetAllAsync();
            tenant.EnforcePlanStaffLimit(existing.Count());

            var existingUser = await _unitOfWork.UserRepository.FindByEmailAsync(tenantId, command.email);
            tenant.EnsureUserEmailIsNotRegistered(existingUser is not null);

            var staff = Staff.Create(
                tenantId: tenantId,
                firstname: command.FirstName,
                lastname: command.LastName,
                email: command.email,
                contactNumber: command.contactNumber,
                capacity: command.Capacity,
                description: command.Description
            );

            _unitOfWork.StaffRepository.Add(staff);

            var fullName = $"{command.FirstName} {command.LastName}";
            var randomPassword = $"{Guid.NewGuid():N}{Guid.NewGuid():N}Aa1@";

            var user = User.CreateInvitedUser(tenantId, fullName, command.email, UserRole.Staff, "pending");

            var createResult = await _unitOfWork.UserRepository.CreateAsync(user, randomPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            await _unitOfWork.UserRepository.AddToRoleAsync(user, "Staff");

            user = await _unitOfWork.UserRepository.FindByEmailAsync(tenantId, command.email);
            if (user is null)
                throw new InvalidOperationException("Error retrieving created user.");

            var setupToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
                user, "PasswordReset", "ResetPassword");

            user.SetInvitationToken(setupToken, DateTime.UtcNow.AddHours(72));

            staff.LinkUser(user.Id);
            _unitOfWork.StaffRepository.Update(staff);

            var admins = await _unitOfWork.UserRepository.GetUsersByRoleAsync(tenantId, UserRole.TenantAdmin);
            foreach (var admin in admins)
            {
                _unitOfWork.NotificationRepository.Add(Notification.Create(
                    admin.Id,
                    NotificationRecipientType.TenantAdmin,
                    tenantId,
                    NotificationEventType.StaffCreated,
                    "Staff Created",
                    $"Staff record for {staff.FirstName} {staff.LastName} has been created."));
            }

            await _unitOfWork.CompleteAsync(ct);

            var setupUrl = _appUrlService.GetSetupAccountUrl(setupToken);

            await _authNotification.SendInvitationEmailAsync(
                command.email,
                fullName,
                tenant.BusinessName,
                UserRole.Staff.ToString(),
                setupUrl,
                ct);

            return staff.ToStaffDto();
        }
    }
}
