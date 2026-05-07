using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Models;
using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Application.Features.Auth.Commands.RefreshToken
{
    internal sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, BaseResponse<RefreshTokenResponseDto>>
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

        public async Task<BaseResponse<RefreshTokenResponseDto>> Handle(RefreshTokenCommand command, CancellationToken ct)
        {
            // 1. Get token from cookie
            var rawToken = _cookieService.GetRefreshTokenFromCookie();
            if (string.IsNullOrEmpty(rawToken)) throw new UnauthorizedAccessException();

            // 2. Find user by this token
            var user = await _unitOfWork.UserRepository.FindByRefreshTokenAsync(rawToken);
            if (user == null) throw new UnauthorizedAccessException();

            // 3. Prepare new token strings
            var newRawToken = _tokenService.GenerateRefreshTokenRaw();
            var utcNow = DateTime.UtcNow;

            // 4. Update Domain State (This stages DB changes)
            user.RotateRefreshToken(rawToken, newRawToken, utcNow);

            // 5. Generate new Access Token
            var role = user.Role.ToString().ToLowerInvariant();
            var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null) throw new UnauthorizedAccessException("Tenant not found.");
            var newAccessToken = _tokenService.GenerateAccessToken(user, role, tenant.Slug);

            // 6. Update Transport (Cookie)
            _cookieService.SetRefreshTokenCookie(newRawToken);

            // 7. Persist everything (One single transaction)
            await _unitOfWork.CompleteAsync(ct);

            // 8. Return Response
            // Note: Per your security requirement, the refresh token 
            // should ideally NOT be in this body if it is already in the cookie.
            return BaseResponse<RefreshTokenResponseDto>.Success(new RefreshTokenResponseDto
            {
                AccessToken = newAccessToken,
                UserId = user.Id,
                TenantId = user.TenantId
            });
        }
    }
}