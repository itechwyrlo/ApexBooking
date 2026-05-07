using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;

namespace ApexBooking.Core.Application.Features.Auth.Commands.Logout
{
    internal sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICookieService _cookieService;
        private readonly IUserContextService _userContext;
        private readonly ITokenService _tokenService;

        public LogoutCommandHandler(
            IUnitOfWork unitOfWork,
            ICookieService cookieService,
            IUserContextService userContext,
            ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _cookieService = cookieService;
            _userContext = userContext;
            _tokenService = tokenService;
        }

        public async Task Handle(LogoutCommand command, CancellationToken ct)
        {
            var userId = _userContext.GetCurrentUserId();
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

            if (user == null) return;

            var refreshTokenValue = _cookieService.GetRefreshTokenFromCookie();

            if (!string.IsNullOrEmpty(refreshTokenValue))
            {
                user.RevokeSpecificToken(refreshTokenValue);
            }

            var jti = _userContext.GetCurrentJti();
            await _tokenService.BlacklistJtiAsync(jti, ct);

            _cookieService.DeleteRefreshTokenCookie();

            await _unitOfWork.CompleteAsync(ct);
        }
    }
}