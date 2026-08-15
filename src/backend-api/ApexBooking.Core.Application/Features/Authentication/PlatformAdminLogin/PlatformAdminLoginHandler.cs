using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Auth;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Authentication.PlatformAdminLogin
{
    // Mirrors TenantLoginHandler's shape, but for the platform-admin surface — no slug, no tenant
    // membership check, just: real credentials, and the account must actually be a platform admin.
    public class PlatformAdminLoginHandler : ICommandHandler<PlatformAdminLoginCommand, LoginResponse>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly ITokenService _tokenService;
        private readonly ICookieService _cookieService;

        public PlatformAdminLoginHandler(
            IApplicationUserService applicationUserService,
            ITokenService tokenService,
            ICookieService cookieService)
        {
            _applicationUserService = applicationUserService;
            _tokenService = tokenService;
            _cookieService = cookieService;
        }

        public async Task<LoginResponse> Handle(PlatformAdminLoginCommand command, CancellationToken cancellationToken)
        {
            await _applicationUserService.LoginAsync(command.Email, command.Password);

            var user = await _applicationUserService.GetUserByEmailAsync(command.Email, cancellationToken);
            if (user is null)
                throw new BusinessRuleBrokenException("The email or password provided is incorrect.");

            // This surface is platform-admin-only — a real tenant account can authenticate (the
            // credentials are valid), but is never allowed to actually sign in through it.
            if (!user.IsPlatformAdmin)
                throw new UnauthorizedException("This account is not authorized for platform administration.");

            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            var refreshTokenRaw = await _applicationUserService.IssueRefreshTokenAsync(user.Id, cancellationToken);
            _cookieService.SetRefreshTokenCookie(refreshTokenRaw, isPlatformAdmin: true);

            var adminDescriptor = new TokenDescriptor(
                UserId: user.Id,
                Email: user.Email,
                FullName: fullName,
                IsPlatformAdmin: true,
                TenantId: null,
                Role: null);

            var adminToken = _tokenService.GenerateAccessToken(adminDescriptor);

            return new LoginResponse(
                Token: adminToken,
                Email: user.Email,
                FullName: fullName,
                IsPlatformAdmin: true);
        }
    }
}
