using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Auth.Commands.Logout
{
    public sealed record LogoutCommand() : ICommand;
}