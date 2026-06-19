using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.Login
{
    internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly ICookieService _cookieService;

        public LoginCommandHandler(ICookieService cookieService, IUnitOfWork unitOfWork, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _cookieService = cookieService;
        }

        public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken ct)
        {
            var user = await _unitOfWork.UserRepository.FindByEmailAcrossAllTenantsAsync(command.Email);
            if (user == null)
                throw new UnauthorizedException("Invalid email or password.");

            if (user.Status != UserStatus.Active)
                throw new UnauthorizedException("Account is not active.");

            if (!user.EmailVerifiedAt.HasValue)
                throw new UnauthorizedException("Please verify your email first.");

            var passwordResult = await _unitOfWork.UserRepository.CheckPasswordAsync(user, command.Password);
            if (!passwordResult)
                throw new UnauthorizedException("Invalid email or password.");

            var role = user.Role.ToString().ToLowerInvariant();

            user.UpdateLastLogin();

            var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null)
                throw new NotFoundException("Tenant not found.");

            var accessToken = _tokenService.GenerateAccessToken(user, role, tenant.Slug);
            var rawRefreshToken = _tokenService.GenerateRefreshTokenRaw();

            _cookieService.SetRefreshTokenCookie(rawRefreshToken);

            user.AddRefreshToken(rawRefreshToken, DateTime.UtcNow);

            await _unitOfWork.CompleteAsync();

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                UserId = user.Id,
                TenantId = user.TenantId,
                TenantSlug = tenant.Slug
            };
        }
    }
}