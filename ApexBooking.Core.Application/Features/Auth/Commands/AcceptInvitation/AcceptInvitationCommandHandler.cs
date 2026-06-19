using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.AcceptInvitation;

internal sealed class AcceptInvitationCommandHandler
    : ICommandHandler<AcceptInvitationCommand, AuthResponseDto>
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

    public async Task<AuthResponseDto> Handle(
        AcceptInvitationCommand command,
        CancellationToken ct)
    {


        var user = await _unitOfWork.UserRepository.FindByInvitationTokenAsync(command.Token);
        if (user is null)
            throw new UnauthorizedException("This invitation link is invalid or has expired.");

        user.EnsureNotYetActivated();

        var tokenValid = await _unitOfWork.UserRepository.VerifyUserTokenAsync(
            user, command.Token, "PasswordReset", "ResetPassword");
        if (!tokenValid)
            throw new UnauthorizedException("This invitation link is invalid or has expired.");

        var freshToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
            user, "PasswordReset", "ResetPassword");

        var resetResult = await _unitOfWork.UserRepository.ResetPasswordAsync(user, freshToken, command.NewPassword);
        if (!resetResult.Succeeded)
            throw new BusinessRuleBrokenException("Password does not meet the required criteria.");

        user = await _unitOfWork.UserRepository.FindByEmailAsync(user.TenantId, user.Email!);
        if (user is null)
            throw new NotFoundException("User not found after password reset.");

        user.MarkEmailVerified();
        user.Activate();

        var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(user.TenantId);
        if (tenant is null)
            throw new NotFoundException("Organization not found.");

        var role = user.Role.ToString().ToLowerInvariant();
        var accessToken = _tokenService.GenerateAccessToken(user, role, tenant.Slug);
        var rawRefreshToken = _tokenService.GenerateRefreshTokenRaw();

        _cookieService.SetRefreshTokenCookie(rawRefreshToken);
        user.AddRefreshToken(rawRefreshToken, DateTime.UtcNow);

        await _unitOfWork.CompleteAsync(ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            UserId = user.Id,
            TenantId = user.TenantId,
            TenantSlug = tenant.Slug
        };
    }
}