using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.UpdateStaff
{
    public sealed record UpdateStaffCommand(
        Guid StaffId,
        string firstName,
        string lastName,
        string email,
        string contactNumber,
        string? Description,
        int Capacity
    ) : ICommand<StaffDto>;
}