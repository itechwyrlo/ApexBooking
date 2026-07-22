using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.RefreshToken
{
    public record RefreshTokenCommand() : ICommand<RefreshTokenResponseDto>;
}