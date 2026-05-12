using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Auth.Commands.ForgotPassword
{
    internal sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, BaseResponse<ForgotPasswordResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notification;
        private readonly IAppUrlService _appUrlService;

        public ForgotPasswordCommandHandler(
            IUnitOfWork unitOfWork,
            INotificationService notification,
            IAppUrlService appUrlService)
        {
            _unitOfWork = unitOfWork;
            _notification = notification;
            _appUrlService = appUrlService;
        }

        public async Task<BaseResponse<ForgotPasswordResponseDto>> Handle(ForgotPasswordCommand command, CancellationToken ct)
        {
            var user = await _unitOfWork.UserRepository.FindByEmailAcrossAllTenantsAsync(command.Email);

            // Email enumeration protection - always return same message
            if (user == null)
                return BaseResponse<ForgotPasswordResponseDto>.Failure("Users doesn't exist");

            var resetToken = await _unitOfWork.UserRepository.GenerateUserTokenAsync(user, "PasswordReset", "ResetPassword");
            var resetUrl = _appUrlService.GetPasswordResetUrl(resetToken);

            var emailBody = $@"
            <h1>Reset Your Password</h1>
            <p>Hi {user.FullName}, click the link below to reset your password:</p>
            <a href='{resetUrl}' style='padding: 10px 20px; background: blue; color: white; text-decoration: none;'>
                Reset Password
            </a>
            <p>If the button doesn't work, copy and paste this link: {resetUrl}</p>
            <p>This link expires in 1 hour.</p>";

            await _notification.SendEmailAsync(user.Email!, "Reset Your ApexBooking Password", emailBody);

            return BaseResponse<ForgotPasswordResponseDto>.Success(new ForgotPasswordResponseDto
            {
                Message = "If that email is registered, you will receive a reset link."
            });
        }
    }
}
