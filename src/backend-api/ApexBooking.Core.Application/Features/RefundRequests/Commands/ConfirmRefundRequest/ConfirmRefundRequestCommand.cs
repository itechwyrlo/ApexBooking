using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest
{
    // Content-type/size are validated by the controller before this is ever dispatched — same
    // edge-validation split as UpdateMyProfilePhotoCommand.
    public record ConfirmRefundRequestCommand(
        Guid RefundRequestId,
        Stream ReceiptContent,
        string ReceiptContentType,
        string ReceiptFileExtension
    ) : ICommand;
}
