using System.Text.Json;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Payment.Commands.HandlePayPalWebhook;

internal sealed class HandlePayPalWebhookHandler
    : ICommandHandler<HandlePayPalWebhookCommand>
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

    public async Task Handle(
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
            throw new BusinessRuleBrokenException("Invalid webhook payload.");
        }

        var eventType = doc.RootElement
            .GetProperty("event_type").GetString();

        if (eventType != "CHECKOUT.ORDER.APPROVED" && eventType != "PAYMENT.CAPTURE.COMPLETED")
            return;

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
            throw new BusinessRuleBrokenException("Order ID not found in webhook payload.");

        var transaction = await _unitOfWork.PaymentTransactionRepository
            .GetAsync(t => t.GatewayTransactionId == orderId);

        if (transaction is null)
            throw new NotFoundException("Payment transaction not found.");

        // TR-11.3: validate webhook signature using the platform-level gateway credentials.
        var platformGateway = await _unitOfWork.PlatformPaymentGatewayRepository.GetActiveAsync(ct);

        if (platformGateway is null)
            throw new NotFoundException("No active payment gateway configured.");

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
            throw new BusinessRuleBrokenException("Webhook signature validation failed.");

        if (transaction.Status == Domain.Enums.PaymentTransactionStatus.Paid)
            return;

        var booking = await _unitOfWork.BookingRepository
            .GetByIdAsync(new BookingId(transaction.BookingId.Value));

        if (booking is null)
            throw new NotFoundException("Booking not found.");

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

        var admins = await _unitOfWork.UserRepository.GetUsersByRoleAsync(booking.TenantId, UserRole.TenantAdmin);
        foreach (var admin in admins)
        {
            _unitOfWork.NotificationRepository.Add(Notification.Create(
                admin.Id,
                NotificationRecipientType.TenantAdmin,
                booking.TenantId,
                NotificationEventType.PaymentCaptured,
                "Payment Received",
                $"Payment received for Booking {booking.BookingReference}."));
        }

        await _unitOfWork.CompleteAsync(ct);
    }
}
