using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Auth.Commands.AcceptInvitation;

internal sealed class AcceptInvitationCommandHandler
    : ICommandHandler<AcceptInvitationCommand, BaseResponse<AuthResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;

    public AcceptInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ICookieService cookieService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _cookieService = cookieService;
    }

    public async Task<BaseResponse<AuthResponseDto>> Handle(
        AcceptInvitationCommand command,
        CancellationToken ct)
    {
        if (command.NewPassword != command.ConfirmPassword)
            return BaseResponse<AuthResponseDto>.Failure("Passwords do not match.");

        // FindByInvitationTokenAsync already enforces InvitationExpiresAt > UtcNow in its WHERE clause
        var user = await _unitOfWork.UserRepository.FindByInvitationTokenAsync(command.Token);
        if (user is null)
            return BaseResponse<AuthResponseDto>.Failure("This invitation link is invalid or has expired.");

        if (user.Status != UserStatus.Invited)
            return BaseResponse<AuthResponseDto>.Failure("This account has already been activated.");

        // Verify the token cryptographically using the same provider as ForgotPassword
        var tokenValid = await _unitOfWork.UserRepository.VerifyUserTokenAsync(
            user, command.Token, "PasswordReset", "ResetPassword");
        if (!tokenValid)
            return BaseResponse<AuthResponseDto>.Failure("This invitation link is invalid or has expired.");

        // Generate a fresh Identity token to set the password (VerifyUserTokenAsync consumes it)
        var freshToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
            user, "PasswordReset", "ResetPassword");

        var resetResult = await _unitOfWork.UserRepository.ResetPasswordAsync(user, freshToken, command.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
            return BaseResponse<AuthResponseDto>.Failure($"Failed to set password: {errors}");
        }

        // Re-fetch after password reset to get a cleanly tracked entity
        user = await _unitOfWork.UserRepository.FindByEmailAsync(user.TenantId, user.Email!);
        if (user is null)
            return BaseResponse<AuthResponseDto>.Failure("User not found after password reset.");

        user.MarkEmailVerified();
        user.Activate();

        var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(user.TenantId);
        if (tenant is null)
            return BaseResponse<AuthResponseDto>.Failure("Organization not found.");

        var role = user.Role.ToString().ToLowerInvariant();
        var accessToken = _tokenService.GenerateAccessToken(user, role, tenant.Slug);
        var rawRefreshToken = _tokenService.GenerateRefreshTokenRaw();

        _cookieService.SetRefreshTokenCookie(rawRefreshToken);
        user.AddRefreshToken(rawRefreshToken, DateTime.UtcNow);

        await _unitOfWork.CompleteAsync(ct);

        return BaseResponse<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            UserId = user.Id,
            TenantId = user.TenantId,
            TenantSlug = tenant.Slug
        });
    }
}
