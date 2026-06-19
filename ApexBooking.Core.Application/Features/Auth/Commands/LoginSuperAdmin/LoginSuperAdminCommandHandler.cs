using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Application.Features.Auth.Commands.LoginSuperAdmin;

internal sealed class LoginSuperAdminCommandHandler
    : ICommandHandler<LoginSuperAdminCommand, AuthResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;

    public LoginSuperAdminCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ICookieService cookieService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _cookieService = cookieService;
    }

    public async Task<AuthResponseDto> Handle(
        LoginSuperAdminCommand command,
        CancellationToken ct)
    {
        var superAdmin = await _unitOfWork.SuperAdminRepository.FindByEmailAsync(command.Email);
        if (superAdmin is null)
            throw new UnauthorizedException("Invalid email or password.");

        superAdmin.EnsureIsActive();

        var hasher = new PasswordHasher<Domain.Entities.SuperAdmin>();
        var result = hasher.VerifyHashedPassword(superAdmin, superAdmin.PasswordHash, command.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid email or password.");

        superAdmin.UpdateLastLogin();

        var accessToken = _tokenService.GenerateAccessToken(superAdmin);
        var rawRefreshToken = _tokenService.GenerateRefreshTokenRaw();

        _cookieService.SetRefreshTokenCookie(rawRefreshToken);

        superAdmin.AddRefreshToken(rawRefreshToken, DateTime.UtcNow);

        _unitOfWork.SuperAdminRepository.Update(superAdmin);
        await _unitOfWork.CompleteAsync(ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            UserId = superAdmin.SuperAdminId.Value,
            RefreshToken = null!,
            TenantId = null!
        };
    }
}
