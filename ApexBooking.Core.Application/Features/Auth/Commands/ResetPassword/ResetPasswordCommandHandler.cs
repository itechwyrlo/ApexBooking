using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.TokenService;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.ResetPassword
{
    internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserContextService _userContext;
        private readonly ITokenService _tokenService;

        public ResetPasswordCommandHandler(
            IUnitOfWork unitOfWork,
            IUserContextService userContext,
            ITokenService tokenService)
        {
            _unitOfWork = unitOfWork;
            _userContext = userContext;
            _tokenService = tokenService;
        }

        public async Task Handle(ResetPasswordCommand command, CancellationToken ct)
        {
            var userId = _userContext.GetCurrentUserId();

            var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (currentUser == null)
                throw new NotFoundException("User not found.");

            var tokenValid = await _unitOfWork.UserRepository.VerifyUserTokenAsync(
                currentUser, command.Token, "PasswordReset", "ResetPassword");
            if (!tokenValid)
                throw new UnauthorizedException("Invalid or expired reset token. Please request a new one.");

            var passwordResetToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(
                currentUser, "PasswordReset", "ResetPassword");

            var result = await _unitOfWork.UserRepository.ResetPasswordAsync(
                currentUser, passwordResetToken, command.NewPassword);
            if (!result.Succeeded)
                throw new BusinessRuleBrokenException("Password does not meet the required criteria.");

            currentUser.RevokeAllRefreshTokens();

            var jti = _userContext.GetCurrentJti();
            if (!string.IsNullOrEmpty(jti))
                await _tokenService.BlacklistJtiAsync(jti, ct);

            await _unitOfWork.CompleteAsync(ct);
        }
    }
}