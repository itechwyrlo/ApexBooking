using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.RefreshToken
{
    internal sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly ICookieService _cookieService;

        public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService, ICookieService cookieService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _cookieService = cookieService;
        }

        public async Task<RefreshTokenResponseDto> Handle(RefreshTokenCommand command, CancellationToken ct)
        {
            var rawToken = _cookieService.GetRefreshTokenFromCookie();
            if (string.IsNullOrEmpty(rawToken))
                throw new UnauthorizedException("Invalid or missing refresh token.");

            var user = await _unitOfWork.UserRepository.FindByRefreshTokenAsync(rawToken);
            if (user == null)
                throw new UnauthorizedException("Invalid or missing refresh token.");

            var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null)
                throw new NotFoundException("Tenant not found.");

            var newRawToken = _tokenService.GenerateRefreshTokenRaw();
            var utcNow = DateTime.UtcNow;

            user.RotateRefreshToken(rawToken, newRawToken, utcNow);

            var role = user.Role.ToString().ToLowerInvariant();
            var newAccessToken = _tokenService.GenerateAccessToken(user, role, tenant.Slug);

            _cookieService.SetRefreshTokenCookie(newRawToken);

            await _unitOfWork.CompleteAsync(ct);

            return new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                UserId = user.Id,
                TenantId = user.TenantId
            };
        }
    }
}