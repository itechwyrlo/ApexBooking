using ApexBooking.Core.Application.Dtos.Request;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.WeeklySchedule
{
    public record UpdateWeeklyScheduleCommand(
        Guid TenantMemberId,
        IReadOnlyCollection<DayScheduleUpdateItem> Schedules
    ) : ICommand;
}