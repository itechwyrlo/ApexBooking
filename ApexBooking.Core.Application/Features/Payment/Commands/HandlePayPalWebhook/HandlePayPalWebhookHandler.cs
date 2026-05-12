using System.Text.Json;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Payment.Commands.HandlePayPalWebhook;

internal sealed class HandlePayPalWebhookHandler
    : ICommandHandler<HandlePayPalWebhookCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayPalWebhookValidator _validator;

    public HandlePayPalWebhookHandler(
        IUnitOfWork unitOfWork,
        IPayPalWebhookValidator validator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<BaseResponse<bool>> Handle(
        HandlePayPalWebhookCommand command,
        CancellationToken ct)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(command.RequestBody);
        }
        catch
        {
            return BaseResponse<bool>.Failure("Invalid webhook payload.", "INVALID_PAYLOAD");
        }

        var eventType = doc.RootElement
            .GetProperty("event_type").GetString();

        if (eventType != "CHECKOUT.ORDER.APPROVED" && eventType != "PAYMENT.CAPTURE.COMPLETED")
            return BaseResponse<bool>.Success(true);

        var orderId = eventType == "PAYMENT.CAPTURE.COMPLETED"
            ? doc.RootElement
                .GetProperty("resource")
                .GetProperty("supplementary_data")
                .GetProperty("related_ids")
                .GetProperty("order_id")
                .GetString()
            : doc.RootElement
                .GetProperty("resource")
                .GetProperty("id")
                .GetString();

        if (string.IsNullOrWhiteSpace(orderId))
            return BaseResponse<bool>.Failure("Order ID not found in webhook payload.", "ORDER_ID_MISSING");

        var transaction = await _unitOfWork.PaymentTransactionRepository
            .GetAsync(t => t.GatewayTransactionId == orderId);

        if (transaction is null)
            return BaseResponse<bool>.Failure("Payment transaction not found.", "TRANSACTION_NOT_FOUND");

        // TR-11.3: validate webhook signature using the platform-level gateway credentials.
        var platformGateway = await _unitOfWork.PlatformPaymentGatewayRepository.GetActiveAsync(ct);

        if (platformGateway is null)
            return BaseResponse<bool>.Failure("No active payment gateway configured.", "NO_GATEWAY");

        var isValid = await _validator.ValidateAsync(
            platformGateway.ClientId,
            platformGateway.SecretKeyEncrypted,
            platformGateway.WebhookId,
            command.RequestBody,
            command.TransmissionId,
            command.TransmissionTime,
            command.CertUrl,
            command.AuthAlgo,
            command.TransmissionSig,
            ct);

        if (!isValid)
            return BaseResponse<bool>.Failure("Webhook signature validation failed.", "INVALID_SIGNATURE");

        if (transaction.Status == Domain.Enums.PaymentTransactionStatus.Paid)
            return BaseResponse<bool>.Success(true);

        var booking = await _unitOfWork.BookingRepository
            .GetByIdAsync(new BookingId(transaction.BookingId.Value));

        if (booking is null)
            return BaseResponse<bool>.Failure("Booking not found.", "BOOKING_NOT_FOUND");

        var paymentMethodType = eventType == "PAYMENT.CAPTURE.COMPLETED"
            ? doc.RootElement
                .GetProperty("resource")
                .TryGetProperty("payment_source", out var ps)
                    ? ps.EnumerateObject().FirstOrDefault().Name
                    : null
            : null;

        transaction.MarkPaid(paymentMethodType, null);
        booking.MarkPaymentCaptured();

        _unitOfWork.PaymentTransactionRepository.Update(transaction);
        _unitOfWork.BookingRepository.Update(booking);
        await _unitOfWork.CompleteAsync(ct);

        return BaseResponse<bool>.Success(true);
    }
}
