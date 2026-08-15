using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.StaffBreaks
{
    public record RemoveStaffBreakCommand(
        Guid TenantMemberId,
        Guid BreakId
    ) : ICommand;
}
