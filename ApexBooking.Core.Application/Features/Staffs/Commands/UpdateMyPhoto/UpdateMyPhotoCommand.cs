using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.UpdateMyPhoto
{
    public sealed record UpdateMyPhotoCommand(string? PhotoUrl) : ICommand<StaffDto>;
}
