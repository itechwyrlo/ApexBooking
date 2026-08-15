using System;
using System.Linq;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using Xunit;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.UnitTests.Entities;

public class BookingRefundTests
{
    private static Booking CreateOnlinePaidBooking(DateOnly scheduledDate, TimeOnly scheduledStartTime, decimal amountDue = 500m)
    {
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-01",
            scheduledDate: scheduledDate,
            scheduledStartTime: scheduledStartTime,
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: true,
            currencyCode: "PHP",
            amountDue: amountDue);

        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_test123");
        booking.ClearDomainEvents(); // isolate the assertions below to the Cancel call itself
        return booking;
    }

    private static PaymentPolicy CreatePaymentPolicy(TenantId tenantId, bool refundEnabled, decimal onTimePercent = 100m, decimal latePercent = 0m)
    {
        var policy = new PaymentPolicy(tenantId);
        policy.UpdatePolicy(
            PaymentRequirementType.None, DepositType.Percentage, 0m,
            onTimeRefundPercent: onTimePercent, lateCancellationRefundPercent: latePercent,
            refundReviewDeadlineDays: 7, refundEnabled: refundEnabled);
        return policy;
    }

    [Fact]
    public void Cancel_RefundDisabled_NeverRaisesRefund_EvenWhenOtherwiseEligible()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future));
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: false);

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, ewalletProvider: null, ewalletNumber: null, ewalletName: null);

        Assert.Equal(RefundStatus.None, booking.RefundStatus);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
    }

    [Fact]
    public void Cancel_RefundEnabled_OnTime_WithoutEwalletDetails_Throws()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future));
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true);

        Assert.Throws<BusinessRuleBrokenException>(() =>
            booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, ewalletProvider: null, ewalletNumber: null, ewalletName: null));
    }

    [Fact]
    public void Cancel_RefundEnabled_OnTime_WithEwalletDetails_RaisesEligibleEventCarryingThem()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true, onTimePercent: 80m);

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, "GCash", "09171234567", "Juan Dela Cruz");

        Assert.Equal(RefundStatus.Pending, booking.RefundStatus);
        var eligibleEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
        Assert.Equal(400m, eligibleEvent.RefundAmount);
        Assert.Equal("GCash", eligibleEvent.EwalletProvider);
        Assert.Equal("09171234567", eligibleEvent.EwalletNumber);
        Assert.Equal("Juan Dela Cruz", eligibleEvent.EwalletName);
    }

    [Fact]
    public void Cancel_RefundEnabled_ZeroOnTimePercent_DoesNotRaiseRefund_EvenWithoutEwalletDetails()
    {
        // A refund amount that clamps to zero never needs e-wallet details — nothing will
        // actually be asked for or transferred, so the "required when eligible" guard doesn't fire.
        var future = DateTime.UtcNow.AddDays(3);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(future), TimeOnly.FromDateTime(future));
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true, onTimePercent: 0m);

        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, null, null, null);

        Assert.Equal(RefundStatus.None, booking.RefundStatus);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
    }

    [Fact]
    public void Cancel_PastCutoff_PartialRefundPolicy_RaisesPercentageAmount()
    {
        var soon = DateTime.UtcNow.AddHours(2);
        var booking = CreateOnlinePaidBooking(DateOnly.FromDateTime(soon), TimeOnly.FromDateTime(soon), amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        bookingPolicy.UpdateSettings(lateCancellationPolicy: CancellationPolicy.PartialRefund);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true, latePercent: 50m);

        booking.Cancel(Guid.NewGuid(), "Late cancel", bookingPolicy, paymentPolicy, "Maya", "09179876543", "Juan Dela Cruz");

        var eligibleEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
        Assert.Equal(250m, eligibleEvent.RefundAmount);
    }

    [Fact]
    public void ConfirmReviewedRefund_SetsRefundStatusRefunded_AndRaisesConfirmedEvent()
    {
        var booking = CreateOnlinePaidBooking(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true);
        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, "GCash", "09171234567", "Juan Dela Cruz");
        booking.ClearDomainEvents();

        booking.ConfirmReviewedRefund(500m, "https://files.example.com/receipts/abc.png");

        Assert.Equal(RefundStatus.Refunded, booking.RefundStatus);
        Assert.Equal(500m, booking.RefundedAmount);
        var confirmedEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundConfirmedDomainEvent>());
        Assert.Equal(500m, confirmedEvent.RefundedAmount);
        Assert.Equal("https://files.example.com/receipts/abc.png", confirmedEvent.ReceiptUrl);
    }

    [Fact]
    public void RejectReviewedRefund_SetsRefundStatusRejected_AndRaisesRejectedEvent()
    {
        var booking = CreateOnlinePaidBooking(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            amountDue: 500m);
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true);
        booking.Cancel(Guid.NewGuid(), "Change of plans", bookingPolicy, paymentPolicy, "GCash", "09171234567", "Juan Dela Cruz");
        booking.ClearDomainEvents();

        booking.RejectReviewedRefund("Outside policy window");

        Assert.Equal(RefundStatus.Rejected, booking.RefundStatus);
        var rejectedEvent = Assert.Single(booking.DomainEvents.OfType<BookingRefundRejectedDomainEvent>());
        Assert.Equal("Outside policy window", rejectedEvent.RejectionReason);
    }

    [Fact]
    public void CancelByCustomer_PayInVisitBooking_NeverRefunds()
    {
        var future = DateTime.UtcNow.AddDays(3);
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-02",
            scheduledDate: DateOnly.FromDateTime(future),
            scheduledStartTime: TimeOnly.FromDateTime(future),
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: false,
            currencyCode: "PHP",
            amountDue: 500m);
        booking.ClearDomainEvents();
        var bookingPolicy = new BookingPolicy(booking.TenantId);
        var paymentPolicy = CreatePaymentPolicy(booking.TenantId, refundEnabled: true);

        booking.CancelByCustomer("Change of plans", bookingPolicy, paymentPolicy, null, null, null);

        Assert.Equal(RefundStatus.None, booking.RefundStatus);
        Assert.Empty(booking.DomainEvents.OfType<BookingRefundEligibleDomainEvent>());
    }
}
