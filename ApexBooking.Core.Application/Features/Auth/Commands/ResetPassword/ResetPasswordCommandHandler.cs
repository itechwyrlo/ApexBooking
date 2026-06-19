using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.ResetPassword
{
    internal sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ResetPasswordCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ResetPasswordCommand command, CancellationToken ct)
        {
            var currentUser = await _unitOfWork.UserRepository.GetByIdAsync(command.UserId);
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

            await _unitOfWork.CompleteAsync(ct);
        }
    }
}
