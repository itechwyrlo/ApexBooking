using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.DeactivateStaff
{
    public sealed record DeactivateStaffCommand(Guid staffId) : ICommand;
}