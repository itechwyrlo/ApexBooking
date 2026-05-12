using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateTenantUser;

internal sealed class CreateTenantUserCommandHandler
    : ICommandHandler<CreateTenantUserCommand, BaseResponse<TenantUserDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notification;
    private readonly IAppUrlService _appUrlService;

    public CreateTenantUserCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationService notification,
        IAppUrlService appUrlService)
    {
        _unitOfWork = unitOfWork;
        _notification = notification;
        _appUrlService = appUrlService;
    }

    public async Task<BaseResponse<TenantUserDto>> Handle(
        CreateTenantUserCommand command,
        CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(command.TenantSlug);
        if (tenant is null)
            return BaseResponse<TenantUserDto>.Failure("Organization not found.");

        var existing = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, command.Email);
        if (existing is not null)
            return BaseResponse<TenantUserDto>.Failure("A user with this email already exists in this organization.");

        if (!Enum.TryParse<UserRole>(command.Role, ignoreCase: true, out var userRole))
            return BaseResponse<TenantUserDto>.Failure($"Invalid role '{command.Role}'.");

        var identityRole = userRole switch
        {
            UserRole.TenantAdmin => "TENANT ADMIN",
            UserRole.Manager => "Manager",
            UserRole.Staff => "Staff",
            UserRole.Customer => "Customer",
            _ => throw new InvalidOperationException($"Unhandled role: {userRole}")
        };

        // Create invited user with a placeholder token — overwritten after Identity creates the record
        var user = User.CreateInvitedUser(tenant.TenantId, command.FullName, command.Email, userRole, "pending");

        // Random unguessable password — the user will set their own via the invitation link
        var randomPassword = $"{Guid.NewGuid():N}{Guid.NewGuid():N}Aa1@";

        var createResult = await _unitOfWork.UserRepository.CreateAsync(user, randomPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return BaseResponse<TenantUserDto>.Failure($"Failed to create user: {errors}");
        }

        await _unitOfWork.UserRepository.AddToRoleAsync(user, identityRole);

        // Re-fetch so the user is in Identity's tracked state for token generation
        user = await _unitOfWork.UserRepository.FindByEmailAsync(tenant.TenantId, command.Email);
        if (user is null)
            return BaseResponse<TenantUserDto>.Failure("Error retrieving created user.");

        // Generate the setup token using the same infrastructure as ForgotPassword
        var setupToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
            user, "PasswordReset", "ResetPassword");

        // Store the token with 72-hour expiry so we can look up the user by it later
        user.SetInvitationToken(setupToken, DateTime.UtcNow.AddHours(72));

        await _unitOfWork.CompleteAsync(cancellationToken);

        var setupUrl = _appUrlService.GetSetupAccountUrl(setupToken);

        var emailBody = $@"
            <p>Hi {command.FullName},</p>
            <p>You have been invited to join <strong>{tenant.BusinessName}</strong> on ApexBooking as <strong>{userRole}</strong>.</p>
            <p>Click the button below to set up your password and activate your account:</p>
            <p>
                <a href='{setupUrl}' style='display:inline-block;padding:10px 20px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:4px;'>
                    Set Up Your Account
                </a>
            </p>
            <p>This link expires in <strong>72 hours</strong>. If it expires, contact your administrator to resend the invitation.</p>
            <p>If you did not expect this invitation, you can safely ignore this email.</p>";

        await _notification.SendEmailAsync(command.Email, $"You've been invited to {tenant.BusinessName}", emailBody);

        return BaseResponse<TenantUserDto>.Success(user.ToTenantUserDto());
    }
}
