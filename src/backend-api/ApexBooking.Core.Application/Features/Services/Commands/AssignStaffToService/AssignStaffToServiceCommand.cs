using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Services.Commands.AssignStaffToService
{
    public record AssignStaffToServiceCommand(
        Guid ServiceId,
        Guid TenantMemberId
    ) : ICommand;
}
