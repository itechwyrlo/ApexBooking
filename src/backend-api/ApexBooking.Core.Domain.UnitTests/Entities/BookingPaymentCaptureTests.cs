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

public class BookingPaymentCaptureTests
{
    private static Booking CreateBooking(bool requiresUpfrontPayment, decimal amountDue, decimal? servicePriceAtBooking)
    {
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-01",
            scheduledDate: DateOnly.FromDateTime(DateTime.UtcNow),
            scheduledStartTime: TimeOnly.FromDateTime(DateTime.UtcNow),
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: requiresUpfrontPayment,
            currencyCode: "PHP",
            amountDue: amountDue,
            servicePriceAtBooking: servicePriceAtBooking);

        booking.ClearDomainEvents();
        return booking;
    }

    [Fact]
    public void RemainingBalance_FullPaymentPaidOnline_IsZero()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 500m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");

        Assert.Equal(0m, booking.RemainingBalance);
    }

    [Fact]
    public void RemainingBalance_DepositPaidOnline_ReflectsTrueGap()
    {
        // A 100 deposit against a 500 service — AmountDue only ever held the deposit.
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 100m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");

        Assert.Equal(400m, booking.RemainingBalance);
    }

    [Fact]
    public void RecordPayInVisitPayment_DepositThenRemainder_CapturesRemainderAndKeepsOnlineFlag()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 100m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");
        booking.ClearDomainEvents();

        booking.RecordPayInVisitPayment();

        Assert.Equal(0m, booking.RemainingBalance);
        Assert.Equal(400m, booking.InVisitAmountCollected);
        Assert.Equal(PaymentConfirmationMethod.Online, booking.PaymentConfirmedVia); // never overwritten
        var captured = Assert.Single(booking.DomainEvents.OfType<PaymentCapturedDomainEvent>());
        Assert.Equal(400m, captured.AmountDue); // amount just captured, not the stale deposit snapshot
    }

    [Fact]
    public void RecordPayInVisitPayment_NothingRemaining_Throws()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 500m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");

        Assert.Throws<BusinessRuleBrokenException>(() => booking.RecordPayInVisitPayment());
    }

    [Fact]
    public void CompleteService_PayAtCounter_CapturesFullPriceAndSetsPayInVisit()
    {
        var booking = CreateBooking(requiresUpfrontPayment: false, amountDue: 300m, servicePriceAtBooking: 300m);

        booking.CompleteService();

        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(PaymentConfirmationMethod.PayInVisit, booking.PaymentConfirmedVia);
        Assert.Equal(300m, booking.InVisitAmountCollected);
        Assert.Equal(0m, booking.RemainingBalance);
        var captured = Assert.Single(booking.DomainEvents.OfType<PaymentCapturedDomainEvent>());
        Assert.Equal(300m, captured.AmountDue);
    }

    [Fact]
    public void CompleteService_AlreadyFullyPaidOnline_DoesNotRaiseAnotherPaymentEvent()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 500m, servicePriceAtBooking: 500m);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");
        booking.ClearDomainEvents();

        booking.CompleteService();

        Assert.Empty(booking.DomainEvents.OfType<PaymentCapturedDomainEvent>());
    }

    [Fact]
    public void RemainingBalance_PreMigrationBookingWithoutServicePrice_FallsBackToAmountDue()
    {
        var booking = CreateBooking(requiresUpfrontPayment: false, amountDue: 250m, servicePriceAtBooking: null);

        Assert.Equal(250m, booking.RemainingBalance);
    }

    [Fact]
    public void RemainingBalance_PreMigrationBookingAlreadyConfirmed_FallsBackToZero()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 250m, servicePriceAtBooking: null);
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");

        Assert.Equal(0m, booking.RemainingBalance);
    }

    [Fact]
    public void ClearPendingPaymentOnArrival_FromPendingPayment_MovesToScheduledWithoutConfirmingPayment()
    {
        var booking = CreateBooking(requiresUpfrontPayment: true, amountDue: 100m, servicePriceAtBooking: 500m);

        booking.ClearPendingPaymentOnArrival();

        Assert.Equal(BookingStatus.Scheduled, booking.Status);
        Assert.Null(booking.PaymentConfirmedVia);
        Assert.Equal(500m, booking.RemainingBalance); // nothing paid online, nothing collected — full price still owed
    }

    [Fact]
    public void ClearPendingPaymentOnArrival_NotPendingPayment_Throws()
    {
        var booking = CreateBooking(requiresUpfrontPayment: false, amountDue: 300m, servicePriceAtBooking: 300m);

        Assert.Throws<BusinessRuleBrokenException>(() => booking.ClearPendingPaymentOnArrival());
    }
}
