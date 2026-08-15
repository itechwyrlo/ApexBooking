using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.TenantRequest.Commands.Approve
{
    public record ApprovedTenantRequestCommand(Guid RequestId) : ICommand;
}
