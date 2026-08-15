using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Account.Commands.ChangeMyPassword
{
    public record ChangeMyPasswordCommand(
        string CurrentPassword,
        string NewPassword,
        string ConfirmPassword
    ) : ICommand;
}
