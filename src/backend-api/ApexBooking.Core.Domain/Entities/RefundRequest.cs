using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

// Persistence record, not an aggregate root — same pattern as OutboxMessage/SmsUsage. Queried
// and written only through IRefundRequestStore, never a generic repository.
public class RefundRequest : ITenantEntity
{
    public Guid Id { get; private set; }
    public TenantId? TenantId { get; private set; }
    public Guid BookingId { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;

    public RefundRequestStatus Status { get; private set; }

    public Guid? DecidedByUserId { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? ReceiptUrl { get; private set; }

    // Always populated at creation — the customer (or staff, cancelling on their behalf)
    // provides these in the same request as the cancellation itself, not as a later follow-up.
    public string CustomerEwalletProvider { get; private set; } = string.Empty;
    public string CustomerEwalletNumber { get; private set; } = string.Empty;
    public string CustomerEwalletName { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected RefundRequest() { }

    public static RefundRequest Create(
        TenantId tenantId,
        Guid bookingId,
        decimal requestedAmount,
        string currencyCode,
        string ewalletProvider,
        string ewalletNumber,
        string ewalletName)
    {
        if (requestedAmount <= 0)
            throw new BusinessRuleBrokenException("Refund request amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(ewalletProvider) || string.IsNullOrWhiteSpace(ewalletNumber) || string.IsNullOrWhiteSpace(ewalletName))
            throw new BusinessRuleBrokenException("E-wallet provider, account number, and account name are all required to create a refund request.");

        return new RefundRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BookingId = bookingId,
            RequestedAmount = requestedAmount,
            CurrencyCode = currencyCode,
            CustomerEwalletProvider = ewalletProvider,
            CustomerEwalletNumber = ewalletNumber,
            CustomerEwalletName = ewalletName,
            Status = RefundRequestStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // Owner or Admin confirms the manual e-wallet transfer already happened, with a receipt as
    // proof — see ConfirmRefundRequestHandler, which saves the uploaded file before calling this.
    public void Confirm(Guid decidedByUserId, string receiptUrl)
    {
        if (Status != RefundRequestStatus.PendingReview)
            throw new BusinessRuleBrokenException("This refund request has already been decided.");

        if (string.IsNullOrWhiteSpace(receiptUrl))
            throw new BusinessRuleBrokenException("A receipt is required to confirm a refund.");

        DecidedByUserId = decidedByUserId;
        DecidedAt = DateTime.UtcNow;
        ReceiptUrl = receiptUrl;
        Status = RefundRequestStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(Guid decidedByUserId, string reason)
    {
        if (Status != RefundRequestStatus.PendingReview)
            throw new BusinessRuleBrokenException("This refund request has already been decided.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new BusinessRuleBrokenException("A reason is required when rejecting a refund request.");

        DecidedByUserId = decidedByUserId;
        DecidedAt = DateTime.UtcNow;
        RejectionReason = reason;
        Status = RefundRequestStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }
}
