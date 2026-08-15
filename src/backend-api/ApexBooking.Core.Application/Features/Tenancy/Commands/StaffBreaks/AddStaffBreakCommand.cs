using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.StaffBreaks
{
    public record AddStaffBreakCommand(
        Guid TenantMemberId,
        string Name,
        TimeOnly StartTime,
        TimeOnly EndTime
    ) : ICommand<Guid>;
}