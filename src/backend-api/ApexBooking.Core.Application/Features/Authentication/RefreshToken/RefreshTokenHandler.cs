using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Auth;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Authentication.RefreshToken
{
    public class RefreshTokenHandler : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
    {
   

        private readonly IApplicationUserService _applicationUserService;
        private readonly ITokenService _tokenService;
        private readonly ICookieService _cookieService;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenHandler(
            IApplicationUserService applicationUserService,
            ITokenService tokenService,
            ICookieService cookieService,
            IUnitOfWork unitOfWork)
        {
            _applicationUserService = applicationUserService;
            _tokenService = tokenService;
            _cookieService = cookieService;
            _unitOfWork = unitOfWork;
        }

        public async Task<RefreshTokenResponse> Handle(
            RefreshTokenCommand command,
            CancellationToken cancellationToken)
        {
            var presentedSecret = _cookieService.GetRefreshTokenFromCookie(command.IsPlatformAdmin);

            if (string.IsNullOrWhiteSpace(presentedSecret))
                throw new UnauthorizedException("Your session has expired. Please sign in again.");


            var rotation = await _applicationUserService.RotateRefreshTokenAsync(
                presentedSecret,
                cancellationToken);

            if (rotation is null)
                    throw Reject(command.IsPlatformAdmin);

            // Defense in depth: the cookie under this name should only ever hold a secret for
            // this kind of account going forward — a mismatch here means a stale/mixed-shape
            // cookie slipped through (e.g. mid-cutover), not a legitimate session for this route.
            if (rotation.IsPlatformAdmin != command.IsPlatformAdmin)
                throw Reject(command.IsPlatformAdmin);

            TenantId? tenantId = null;
            SystemRole? role = null;
            string? slug = null;
            TenantMemberId? tenantMemberId = null;

            if (!rotation.IsPlatformAdmin)
            {
                var tenant = await _unitOfWork.TenantRepository.GetByUserIdAsync(
                    rotation.UserId,
                    cancellationToken);

                if (tenant is null || !tenant.IsActive)
                    throw Reject(command.IsPlatformAdmin);

                var membership = tenant.Members.FirstOrDefault(m => m.UserId == rotation.UserId);

                if (membership is null)
                    throw Reject(command.IsPlatformAdmin);

                tenantId = tenant.TenantId;
                role = membership.Role;
                slug = tenant.Slug;
                tenantMemberId = membership.TenantMemberId;
            }

            var accessToken = _tokenService.GenerateAccessToken(
                new TokenDescriptor(
                    rotation.UserId,
                    rotation.Email,
                    rotation.FullName,
                    rotation.IsPlatformAdmin,
                    tenantId,
                    role,
                    slug,
                    tenantMemberId));

            _cookieService.SetRefreshTokenCookie(rotation.RefreshTokenSecret, command.IsPlatformAdmin);

            return new RefreshTokenResponse(accessToken);
        }

        private UnauthorizedException Reject(bool isPlatformAdmin)
        {
            _cookieService.DeleteRefreshTokenCookie(isPlatformAdmin);
            return new UnauthorizedException("Your session has expired. Please sign in again.");
        }
    }
}
