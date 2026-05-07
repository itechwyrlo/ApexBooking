using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Cookie;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Application.Features.Auth.Commands.Login
{
    internal sealed class LoginCommandHandler : ICommandHandler<LoginCommand, BaseResponse<AuthResponseDto>>
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

        public async Task<BaseResponse<AuthResponseDto>> Handle(LoginCommand command, CancellationToken ct)
        {
            // Find user by email using UserRepository
            var user = await _unitOfWork.UserRepository.FindByEmailAcrossAllTenantsAsync(command.Email);
            if (user == null)
                return BaseResponse<AuthResponseDto>.Failure("Invalid email or password.");

            // Check if user is active
            if (user.Status != UserStatus.Active)
                return BaseResponse<AuthResponseDto>.Failure("Account is not active.");

            // Check if email is verified
            if (!user.EmailVerifiedAt.HasValue)
                return BaseResponse<AuthResponseDto>.Failure("Please verify your email first.");

            // Validate password
            var passwordResult = await _unitOfWork.UserRepository.CheckPasswordAsync(user, command.Password);
            if (!passwordResult)
            {
                return BaseResponse<AuthResponseDto>.Failure("Invalid email or password.");
            }

            // Get user role
            var role = user.Role.ToString().ToLowerInvariant();

            // Update last login
            user.UpdateLastLogin();

            // Get tenant information
            var tenant = await _unitOfWork.TenantRepository.GetByIdAsync(user.TenantId);
            if (tenant == null)
            {
                return BaseResponse<AuthResponseDto>.Failure("Tenant not found.");
            }

            // Generate access token with MVP claims
            var accessToken = _tokenService.GenerateAccessToken(user, role, tenant.Slug);
            var rawRefreshToken = _tokenService.GenerateRefreshTokenRaw();

            _cookieService.SetRefreshTokenCookie(rawRefreshToken);

            user.AddRefreshToken(rawRefreshToken, DateTime.UtcNow);

            await _unitOfWork.CompleteAsync();

            return BaseResponse<AuthResponseDto>.Success(new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                UserId = user.Id,
                TenantId = user.TenantId,
                TenantSlug = tenant.Slug
            });
        }
    }
}
