using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateTenantUser;

internal sealed class CreateTenantUserCommandHandler : ICommandHandler<CreateTenantUserCommand, TenantUserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthNotificationService _authNotification;
    private readonly IAppUrlService _appUrlService;

    public CreateTenantUserCommandHandler(
        IUnitOfWork unitOfWork,
        IAuthNotificationService authNotification,
        IAppUrlService appUrlService)
    {
        _unitOfWork = unitOfWork;
        _authNotification = authNotification;
        _appUrlService = appUrlService;
    }

    public async Task<TenantUserDto> Handle(CreateTenantUserCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(command.TenantSlug);
        if (tenant is null)
            throw new NotFoundException("Organization not found.");

        var existing = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, command.Email);
        tenant.EnsureUserEmailIsNotRegistered(existing is not null);

        var userRole = Enum.Parse<UserRole>(command.Role, ignoreCase: true);

        var identityRole = userRole switch
        {
            UserRole.TenantAdmin => "TENANT ADMIN",
            UserRole.Manager => "Manager",
            UserRole.Staff => "Staff",
            UserRole.Customer => "Customer",
            _ => throw new InvalidOperationException($"Unhandled role: {userRole}")
        };

        var user = User.CreateInvitedUser(tenant.TenantId, command.FullName, command.Email, userRole, "pending");

        var randomPassword = $"{Guid.NewGuid():N}{Guid.NewGuid():N}Aa1@";

        var createResult = await _unitOfWork.UserRepository.CreateAsync(user, randomPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        await _unitOfWork.UserRepository.AddToRoleAsync(user, identityRole);

        user = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, command.Email);
        if (user is null)
            throw new InvalidOperationException("Error retrieving created user.");

        var setupToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
            user, "PasswordReset", "ResetPassword");

        user.SetInvitationToken(setupToken, DateTime.UtcNow.AddHours(72));
        await _unitOfWork.CompleteAsync(cancellationToken);

        var setupUrl = _appUrlService.GetSetupAccountUrl(setupToken);

        await _authNotification.SendInvitationEmailAsync(
            command.Email,
            command.FullName,
            tenant.BusinessName,
            userRole.ToString(),
            setupUrl,
            cancellationToken);

        return user.ToTenantUserDto();
    }
}
