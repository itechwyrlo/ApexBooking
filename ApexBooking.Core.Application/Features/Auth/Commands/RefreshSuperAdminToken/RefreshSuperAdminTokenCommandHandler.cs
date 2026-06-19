using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.RefreshSuperAdminToken;

internal sealed class RefreshSuperAdminTokenCommandHandler
    : ICommandHandler<RefreshSuperAdminTokenCommand, RefreshSuperAdminTokenResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ICookieService _cookieService;

    public RefreshSuperAdminTokenCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ICookieService cookieService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _cookieService = cookieService;
    }

    public async Task<RefreshSuperAdminTokenResponseDto> Handle(
        RefreshSuperAdminTokenCommand command,
        CancellationToken ct)
    {
        var rawToken = _cookieService.GetRefreshTokenFromCookie();
        if (string.IsNullOrEmpty(rawToken))
            throw new UnauthorizedException("Invalid or missing refresh token.");

        var superAdmin = await _unitOfWork.SuperAdminRepository.FindByRefreshTokenAsync(rawToken);
        if (superAdmin is null)
            throw new UnauthorizedException("Invalid or missing refresh token.");

        var newRawToken = _tokenService.GenerateRefreshTokenRaw();
        var utcNow = DateTime.UtcNow;

        superAdmin.RotateRefreshToken(rawToken, newRawToken, utcNow);

        var newAccessToken = _tokenService.GenerateAccessToken(superAdmin);

        _cookieService.SetRefreshTokenCookie(newRawToken);

        _unitOfWork.SuperAdminRepository.Update(superAdmin);
        await _unitOfWork.CompleteAsync(ct);

        return new RefreshSuperAdminTokenResponseDto
        {
            AccessToken = newAccessToken,
            UserId = superAdmin.SuperAdminId.Value
        };
    }
}
