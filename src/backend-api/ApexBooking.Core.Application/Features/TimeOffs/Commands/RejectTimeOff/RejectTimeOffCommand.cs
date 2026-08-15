using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.TimeOffs.Commands.RejectTimeOff
{
    public record RejectTimeOffCommand(Guid TimeOffRequestId) : ICommand;
}
