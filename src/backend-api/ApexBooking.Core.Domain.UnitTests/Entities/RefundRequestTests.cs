using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;
using Xunit;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class RefundRequestTests
{
    private static RefundRequest CreatePendingReview() =>
        RefundRequest.Create(
            new TenantId(Guid.NewGuid()), Guid.NewGuid(), 500m, "PHP",
            ewalletProvider: "GCash", ewalletNumber: "09171234567", ewalletName: "Juan Dela Cruz");

    [Fact]
    public void Create_StartsInPendingReview_WithEwalletDetailsAttached()
    {
        var request = CreatePendingReview();

        Assert.Equal(RefundRequestStatus.PendingReview, request.Status);
        Assert.Equal("GCash", request.CustomerEwalletProvider);
        Assert.Equal("09171234567", request.CustomerEwalletNumber);
        Assert.Equal("Juan Dela Cruz", request.CustomerEwalletName);
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        Assert.Throws<BusinessRuleBrokenException>(() =>
            RefundRequest.Create(new TenantId(Guid.NewGuid()), Guid.NewGuid(), 0m, "PHP", "GCash", "09171234567", "Juan Dela Cruz"));
    }

    [Fact]
    public void Confirm_FromPendingReview_MovesToRefunded_WithReceiptUrl()
    {
        var request = CreatePendingReview();
        var userId = Guid.NewGuid();

        request.Confirm(userId, "https://files.example.com/receipts/abc.png");

        Assert.Equal(RefundRequestStatus.Refunded, request.Status);
        Assert.Equal(userId, request.DecidedByUserId);
        Assert.Equal("https://files.example.com/receipts/abc.png", request.ReceiptUrl);
        Assert.NotNull(request.DecidedAt);
    }

    [Fact]
    public void Confirm_WhenAlreadyDecided_Throws()
    {
        var request = CreatePendingReview();
        request.Confirm(Guid.NewGuid(), "https://files.example.com/receipts/abc.png");

        Assert.Throws<BusinessRuleBrokenException>(() =>
            request.Confirm(Guid.NewGuid(), "https://files.example.com/receipts/def.png"));
    }

    [Fact]
    public void Reject_FromPendingReview_MovesToRejected_WithReason()
    {
        var request = CreatePendingReview();
        var userId = Guid.NewGuid();

        request.Reject(userId, "Customer no-showed twice");

        Assert.Equal(RefundRequestStatus.Rejected, request.Status);
        Assert.Equal(userId, request.DecidedByUserId);
        Assert.Equal("Customer no-showed twice", request.RejectionReason);
    }

    [Fact]
    public void Reject_WithoutReason_Throws()
    {
        var request = CreatePendingReview();

        Assert.Throws<BusinessRuleBrokenException>(() =>
            request.Reject(Guid.NewGuid(), ""));
    }

    [Fact]
    public void Reject_WhenAlreadyDecided_Throws()
    {
        var request = CreatePendingReview();
        request.Reject(Guid.NewGuid(), "Not eligible");

        Assert.Throws<BusinessRuleBrokenException>(() =>
            request.Reject(Guid.NewGuid(), "Second reason"));
    }
}
