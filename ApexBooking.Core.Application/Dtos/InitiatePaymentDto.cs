namespace ApexBooking.Core.Application.Dtos
{
    public record InitiatePaymentDto(
        string ApprovalUrl,
        string GatewayTransactionId,
        string BookingReference
    );
}