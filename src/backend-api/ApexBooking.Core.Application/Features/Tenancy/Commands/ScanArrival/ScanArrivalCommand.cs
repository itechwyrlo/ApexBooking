using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.ScanArrival
{
    public record ScanArrivalCommand(string Token) : ICommand<ScanArrivalResult>;
}
