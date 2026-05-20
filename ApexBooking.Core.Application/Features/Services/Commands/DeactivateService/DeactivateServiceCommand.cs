using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Services.Commands.DeactivateService
{
    public sealed record DeactivateServiceCommand(Guid ServiceId) : ICommand;
}