using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Authentication.ResetPassword
{
    internal sealed class ResetPasswordHandler
        : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppUrlService _appUrlService;
        private readonly ICookieService _cookieService;

        public ResetPasswordHandler(
            IApplicationUserService applicationUserService,
            IUnitOfWork unitOfWork,
            IAppUrlService appUrlService,
            ICookieService cookieService)
        {
            _applicationUserService = applicationUserService;
            _unitOfWork = unitOfWork;
            _appUrlService = appUrlService;
            _cookieService = cookieService;
        }

        public async Task<ResetPasswordResult> Handle(
            ResetPasswordCommand command,
            CancellationToken cancellationToken)
        {
            var user = await _applicationUserService.GetUserAsync(command.UserId, cancellationToken);

            if (user is null)
                throw new BusinessRuleBrokenException("This link is no longer valid. Request a new one.");

           
            var tenant = await _unitOfWork.TenantRepository.GetByUserIdAsync(user.UserId, cancellationToken);

            var membership = tenant?.Members
                .FirstOrDefault(m => m.UserId == user.UserId);

            var session = await _applicationUserService.ResetPasswordAsync(
                command.UserId,
                command.Token,
                command.NewPassword,
                tenant?.TenantId,
                membership?.Role,
                tenant?.Slug,
                membership?.TenantMemberId,
                cancellationToken);

            _cookieService.SetRefreshTokenCookie(session.RefreshToken, isPlatformAdmin: false);

            var redirectUrl = user.IsPlatformAdmin
                ? _appUrlService.GetPlatformDashboardUrl()
                : _appUrlService.GetTenantDashboardUrl(tenant?.Slug);

            return new ResetPasswordResult(
                session.AccessToken,
                redirectUrl,
                session.Email,
                session.FullName);
        }
    }
}
