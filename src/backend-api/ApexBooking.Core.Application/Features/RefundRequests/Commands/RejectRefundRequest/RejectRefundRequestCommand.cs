using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest
{
    public record RejectRefundRequestCommand(Guid RefundRequestId, string Reason) : ICommand;
}
