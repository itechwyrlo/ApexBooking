using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Commands.UpdateMyProfile
{
    // Email is intentionally absent — it stays immutable for every role, matching
    // UpdateTeamMemberCommand's existing precedent.
    public record UpdateMyProfileCommand(
        string FirstName,
        string LastName,
        string? PhoneNumber
    ) : ICommand;
}
