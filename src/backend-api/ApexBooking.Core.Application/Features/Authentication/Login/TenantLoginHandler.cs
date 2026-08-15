using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Auth;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Authentication.Login
{
    public class TenantLoginHandler : ICommandHandler<TenantLoginCommand, LoginResponse>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICookieService _cookieService;
        private readonly ITenantEntity _tenantEntity;

        public TenantLoginHandler(
            IApplicationUserService applicationUserService,
            ITokenService tokenService,
            IUnitOfWork unitOfWork,
            ICookieService cookieService,
            ITenantEntity tenantEntity)
        {
            _applicationUserService = applicationUserService;
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
            _cookieService = cookieService;
            _tenantEntity = tenantEntity;
        }

        public async Task<LoginResponse> Handle(TenantLoginCommand command, CancellationToken cancellationToken)
        {
            // 1. Authenticate credentials against Identity.
            await _applicationUserService.LoginAsync(command.Email, command.Password);

            var user = await _applicationUserService.GetUserByEmailAsync(command.Email, cancellationToken);
            if (user is null)
                throw new BusinessRuleBrokenException("The email or password provided is incorrect.");

            // 2. This surface is tenant-only. A platform admin account can authenticate here (the
            // credentials are real), but is never allowed to actually sign in through it.
            if (user.IsPlatformAdmin)
                throw new UnauthorizedException("This account is not authorized to sign in through a business workspace.");

            // 3. The slug in the URL must have resolved to a real, active tenant. TenantMiddleware
            // sets this from the {slug} route segment before this handler ever runs (same mechanism
            // the public booking wizard uses for anonymous traffic) — a bad/unknown slug means no
            // ambient tenant was ever set.
            var resolvedTenantId = _tenantEntity.TenantId
                ?? throw new UnauthorizedException("This business workspace could not be found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == resolvedTenantId,
                includes: [t => t.Members]);

            if (tenant is null || !tenant.IsActive)
                throw new UnauthorizedException("This business workspace could not be found.");

            // 4. The account must belong to THIS specific tenant — not just any tenant. This is the
            // actual fix: a valid account for one business can no longer authenticate against
            // another business's login URL just because the credentials happen to be correct.
            var membership = tenant.Members.FirstOrDefault(m => m.UserId == user.Id);
            if (membership is null)
                throw new UnauthorizedException("Your account is not part of this business workspace.");

            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            var refreshTokenRaw = await _applicationUserService.IssueRefreshTokenAsync(user.Id, cancellationToken);
            _cookieService.SetRefreshTokenCookie(refreshTokenRaw, isPlatformAdmin: false);

            var tenantDescriptor = new TokenDescriptor(
                UserId: user.Id,
                Email: user.Email,
                FullName: fullName,
                IsPlatformAdmin: false,
                TenantId: tenant.TenantId,
                Role: membership.Role,
                Slug: tenant.Slug,
                TenantMemberId: membership.TenantMemberId);

            var userToken = _tokenService.GenerateAccessToken(tenantDescriptor);

            return new LoginResponse(
                Token: userToken,
                Email: user.Email,
                FullName: fullName,
                IsPlatformAdmin: false,
                TenantId: tenant.TenantId.Value,
                TenantRole: membership.Role.ToString());
        }
    }
}
