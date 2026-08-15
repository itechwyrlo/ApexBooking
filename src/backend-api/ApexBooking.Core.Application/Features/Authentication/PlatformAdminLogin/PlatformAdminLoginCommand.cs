using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Authentication.PlatformAdminLogin
{
    public record PlatformAdminLoginCommand(string Email, string Password) : ICommand<LoginResponse>;
}
