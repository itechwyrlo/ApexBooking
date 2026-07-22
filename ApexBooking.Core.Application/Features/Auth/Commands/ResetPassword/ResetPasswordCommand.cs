using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword, string ConfirmPassword) : ICommand;
}
