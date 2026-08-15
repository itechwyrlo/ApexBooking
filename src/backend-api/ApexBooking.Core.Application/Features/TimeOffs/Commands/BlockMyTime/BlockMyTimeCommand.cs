using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.TimeOffs.Commands.BlockMyTime
{
    // Self-service only, same as RequestTimeOffCommand — always for the caller's own TenantMember,
    // resolved server-side, never client-supplied. Always today, always a partial-day window (this
    // is a short break, not a leave request), and lands pre-approved — see the handler.
    public record BlockMyTimeCommand(
        DateOnly Date,
        TimeOnly StartTime,
        TimeOnly EndTime,
        string? Reason
    ) : ICommand<Guid>;
}
