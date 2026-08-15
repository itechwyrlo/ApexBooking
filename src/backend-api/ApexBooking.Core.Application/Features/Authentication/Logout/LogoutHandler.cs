using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Cookie;

namespace ApexBooking.Core.Application.Features.Authentication.Logout
{
    public class LogoutHandler : ICommandHandler<LogoutCommand>
    {
        private readonly IUserContextService _userContext;
        private readonly IApplicationUserService _applicationUserService;
        private readonly ICookieService _cookieService;

        public LogoutHandler(
            IUserContextService userContext,
            IApplicationUserService applicationUserService,
            ICookieService cookieService)
        {
            _userContext = userContext;
            _applicationUserService = applicationUserService;
            _cookieService = cookieService;
        }

        public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
        {
            var userId = _userContext.GetCurrentUserId();

            await _applicationUserService.RevokeAllRefreshTokensAsync(userId, cancellationToken);

            _cookieService.DeleteRefreshTokenCookie(_userContext.IsPlatformAdmin());
        }
    }
}
