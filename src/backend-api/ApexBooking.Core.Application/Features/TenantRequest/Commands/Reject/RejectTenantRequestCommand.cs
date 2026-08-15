using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.TenantRequest.Commands.Reject
{
    public record RejectTenantRequestCommand(Guid RequestId, string Reason) : ICommand;
}
