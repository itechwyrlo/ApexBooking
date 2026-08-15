using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Services.Commands.UnassignStaffFromService
{
    public record UnassignStaffFromServiceCommand(
        Guid ServiceId,
        Guid TenantMemberId
    ) : ICommand;
}
