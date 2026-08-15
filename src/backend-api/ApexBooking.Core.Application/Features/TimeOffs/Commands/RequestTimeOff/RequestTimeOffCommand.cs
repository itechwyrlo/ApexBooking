using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.TimeOffs.Commands.RequestTimeOff
{
    // Self-service only — always requested for the caller's own TenantMember, resolved from the
    // authenticated user, never client-supplied. No on-behalf-of submission.
    public record RequestTimeOffCommand(
        TimeOffType Type,
        DateOnly StartDate,
        DateOnly EndDate,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        string? Reason
    ) : ICommand<Guid>;
}
