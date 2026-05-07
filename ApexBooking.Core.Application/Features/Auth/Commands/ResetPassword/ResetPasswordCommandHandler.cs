using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ApexBooking.Core.Application.Features.Auth.Commands.ResetPassword
{
    internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, BaseResponse<AuthResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITokenService _tokenService;

        public ResetPasswordCommandHandler(IUnitOfWork unitOfWork, IUserContextService userContext, ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tokenService = tokenService;
        }

        public async Task<BaseResponse<AuthResponseDto>> Handle(ResetPasswordCommand command, CancellationToken ct)
        {
            
            var userId = _userContext.GetCurrentUserId();
            // Validate passwords match
            if (command.NewPassword != command.ConfirmPassword)
                return BaseResponse<AuthResponseDto>.Failure("Passwords do not match");
            
            // Get current authenticated user
            var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (currentUser == null)
                return BaseResponse<AuthResponseDto>.Failure("User not found");
            
            // Reset password using UserManager through repository
            var resetTokenResult = await _unitOfWork.UserRepository.VerifyUserTokenAsync(currentUser, command.Token, "PasswordReset", "ResetPassword");
            if (!resetTokenResult)
                return BaseResponse<AuthResponseDto>.Failure("Invalid or expired reset token. Please request a new one.");

            // Generate password reset token and change password
            var passwordResetToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(currentUser, "PasswordReset", "ResetPassword");
            var result = await _unitOfWork.UserRepository.ResetPasswordAsync(currentUser, passwordResetToken, command.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BaseResponse<AuthResponseDto>.Failure($"Failed to reset password: {errors}");
            }
            
            // Invalidate all existing sessions
            currentUser.RevokeAllRefreshTokens();
            
            // Blacklist current JWT token
            var jti = _userContext.GetCurrentJti();
            if (!string.IsNullOrEmpty(jti))
            {
                await _tokenService.BlacklistJtiAsync(jti, ct);
            }
            
            await _unitOfWork.CompleteAsync(ct);

            return BaseResponse<AuthResponseDto>.Success(new AuthResponseDto
            {
                AccessToken = null,
                RefreshToken = null,
                UserId = currentUser.Id,
                TenantId = currentUser.TenantId
            });
            
        }
    }
}
