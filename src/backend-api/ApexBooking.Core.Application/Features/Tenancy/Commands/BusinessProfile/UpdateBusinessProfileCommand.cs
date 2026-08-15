using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.BusinessProfile
{
    public record UpdateBusinessProfileCommand(
        string BusinessName,
        string? Description,
        string? LogoUrl,
        string? ContactPhoneNumber
    ) : ICommand;
}
