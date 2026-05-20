using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.CreateStaff
{
    public record CreateStaffCommand(
        string FirstName,
        string LastName,
        string email,
        string contactNumber,
        int Capacity,
        string? Description
    ) : ICommand<StaffDto>;
}