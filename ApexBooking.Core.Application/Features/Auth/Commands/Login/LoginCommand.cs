using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.Login
{
    public record LoginCommand(string Email, string Password) : ICommand<AuthResponseDto>;
}