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

public class BookingExpirationTests
{
    private static Booking CreatePendingPaymentBooking()
    {
        var booking = Booking.Create(
            tenantId: new TenantId(Guid.NewGuid()),
            branchId: new BranchId(Guid.NewGuid()),
            customerId: new CustomerId(Guid.NewGuid()),
            staffId: new TenantMemberId(Guid.NewGuid()),
            serviceId: new ServiceId(Guid.NewGuid()),
            bookingReference: "APX-TEST-02",
            scheduledDate: DateOnly.FromDateTime(DateTime.UtcNow),
            scheduledStartTime: TimeOnly.FromDateTime(DateTime.UtcNow),
            durationMinutes: 30,
            bufferAfterMinutes: 0,
            customerNotes: null,
            requiresUpfrontPayment: true,
            currencyCode: "PHP",
            amountDue: 500m,
            servicePriceAtBooking: 500m);

        booking.ClearDomainEvents();
        return booking;
    }

    [Fact]
    public void ExpirePendingPayment_WhenPendingPayment_SetsExpiredAndRaisesEvent()
    {
        var booking = CreatePendingPaymentBooking();

        booking.ExpirePendingPayment();

        Assert.Equal(BookingStatus.Expired, booking.Status);
        var raised = Assert.Single(booking.DomainEvents.OfType<BookingExpiredDomainEvent>());
        Assert.Equal(booking.BookingId.Value, raised.BookingId);
        Assert.Equal(booking.TenantId, raised.TenantId);
        Assert.Equal(booking.CustomerId.Value, raised.CustomerId);
        Assert.Equal(booking.BookingReference, raised.BookingReference);
    }

    [Fact]
    public void ExpirePendingPayment_WhenNotPendingPayment_Throws()
    {
        var booking = CreatePendingPaymentBooking();
        booking.ConfirmPayment(PaymentConfirmationMethod.Online, "pay_1");
        booking.ClearDomainEvents();

        Assert.Throws<BusinessRuleBrokenException>(() => booking.ExpirePendingPayment());
        Assert.Equal(BookingStatus.Scheduled, booking.Status);
    }
}
