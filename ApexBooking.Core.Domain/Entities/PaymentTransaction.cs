using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class PaymentTransaction : IAggregateRoot, ITenantEntity
{
    public PaymentTransactionId PaymentTransactionId { get; protected set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public BookingId BookingId { get; private set; } = default!;
    public GatewayProvider GatewayProvider { get; private set; }
    public string GatewayTransactionId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public PaymentTransactionStatus Status { get; private set; }
    public string? PaymentMethodType { get; private set; }
    public string? PaymentMethodLast4 { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected PaymentTransaction() { }

    private PaymentTransaction(
        TenantId tenantId,
        BookingId bookingId,
        GatewayProvider gatewayProvider,
        string gatewayTransactionId,
        decimal amount,
        string currencyCode)
    {
        PaymentTransactionId = new PaymentTransactionId(Guid.NewGuid());
        TenantId = tenantId;
        BookingId = bookingId;
        GatewayProvider = gatewayProvider;
        GatewayTransactionId = gatewayTransactionId;
        Amount = amount;
        CurrencyCode = currencyCode;
        Status = PaymentTransactionStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static PaymentTransaction Create(
        TenantId tenantId,
        BookingId bookingId,
        GatewayProvider gatewayProvider,
        string gatewayTransactionId,
        decimal amount,
        string currencyCode)
    {
        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");

        if (bookingId is null)
            throw new BusinessRuleBrokenException("Booking is required.");

        if (string.IsNullOrWhiteSpace(gatewayTransactionId))
            throw new BusinessRuleBrokenException("Gateway transaction ID is required.");

        if (amount <= 0)
            throw new BusinessRuleBrokenException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            throw new BusinessRuleBrokenException("A valid ISO 4217 currency code is required.");

        return new PaymentTransaction(
            tenantId,
            bookingId,
            gatewayProvider,
            gatewayTransactionId,
            amount,
            currencyCode.ToUpperInvariant());
    }

    public void MarkPaid(string? paymentMethodType, string? paymentMethodLast4)
    {
        if (Status != PaymentTransactionStatus.Pending)
            throw new BusinessRuleBrokenException("Only pending transactions can be marked as paid.");

        Status = PaymentTransactionStatus.Paid;
        PaymentMethodType = paymentMethodType;
        PaymentMethodLast4 = paymentMethodLast4;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        if (Status != PaymentTransactionStatus.Pending)
            throw new BusinessRuleBrokenException("Only pending transactions can be marked as failed.");

        Status = PaymentTransactionStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}