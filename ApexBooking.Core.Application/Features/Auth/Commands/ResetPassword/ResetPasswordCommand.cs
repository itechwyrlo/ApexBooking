using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Auth.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(string Token, string NewPassword, string ConfirmPassword) : ICommand;
}
