using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.ForgotPassword
{
    internal sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, ForgotPasswordResponseDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthNotificationService _authNotification;
        private readonly IAppUrlService _appUrlService;

        public ForgotPasswordCommandHandler(
            IUnitOfWork unitOfWork,
            IAuthNotificationService authNotification,
            IAppUrlService appUrlService)
        {
            _unitOfWork = unitOfWork;
            _authNotification = authNotification;
            _appUrlService = appUrlService;
        }

        public async Task<ForgotPasswordResponseDto> Handle(ForgotPasswordCommand command, CancellationToken ct)
        {
            var user = await _unitOfWork.UserRepository.FindByEmailAcrossAllTenantsAsync(command.Email);

            if (user == null)
                throw new NotFoundException("Users doesn't exist");

            var resetToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(user, "PasswordReset", "ResetPassword");
            var resetUrl = _appUrlService.GetPasswordResetUrl(user.Id.ToString(), resetToken);

            await _authNotification.SendPasswordResetEmailAsync(user.Email!, user.FullName, resetUrl, ct);

            return new ForgotPasswordResponseDto
            {
                Message = "If that email is registered, you will receive a reset link."
            };
        }
    }
}
