using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Auth.Commands.ForgotPassword
{
    public sealed record ForgotPasswordCommand(string Email) : ICommand<ForgotPasswordResponseDto>;
}
