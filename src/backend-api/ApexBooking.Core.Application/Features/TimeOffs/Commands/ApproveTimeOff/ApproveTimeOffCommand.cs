using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.TimeOffs.Commands.ApproveTimeOff
{
    public record ApproveTimeOffCommand(Guid TimeOffRequestId) : ICommand;
}
