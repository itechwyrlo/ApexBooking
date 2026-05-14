using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class Booking : IAggregateRoot, ITenantEntity
{
    public BookingId BookingId { get; protected set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public string BookingReference { get; private set; } = string.Empty;
    public ServiceId ServiceId { get; private set; } = default!;
    public string ServiceName { get; private set; } = string.Empty;
    public StaffId StaffId { get; private set; } = default!;
    public string ResourceName { get; private set; } = string.Empty;

    public DateOnly ScheduledDate { get; private set; }
    public TimeOnly ScheduledStartTime { get; private set; }
    public TimeOnly ScheduledEndTime { get; private set; }

    /// <summary>
    /// Snapshot of service duration at time of booking.
    /// TR-10.1 Step 4
    /// </summary>
    public int DurationMinutes { get; private set; }

    public BookingStatus Status { get; private set; }
    public BookingConfirmationMode ConfirmationMode { get; private set; }

    /// <summary>
    /// Snapshot of service price at time of booking. TR-10.1
    /// </summary>
    public decimal PriceSnapshot { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;
    public string? CustomerNotes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }

    /// <summary>
    /// Self-reference for rescheduled bookings. Scale feature — nullable in MVP.
    /// </summary>
    public BookingId? RescheduledFromBookingId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Children
    private readonly List<BookingStatusLog> _statusLogs = new();
    public IReadOnlyCollection<BookingStatusLog> StatusLogs => _statusLogs.AsReadOnly();
    public Guest Guest { get; private set; } = default!;

    protected Booking() { }

    private Booking(
        TenantId tenantId,
        string bookingReference,
        ServiceId serviceId,
        string serviceName,
        StaffId staffId,
        string resourceName,
        DateOnly scheduledDate,
        TimeOnly scheduledStartTime,
        TimeOnly scheduledEndTime,
        int durationMinutes,
        BookingConfirmationMode confirmationMode,
        decimal priceSnapshot,
        string currencyCode,
        string? customerNotes)
    {
        BookingId = new BookingId(Guid.NewGuid());
        TenantId = tenantId;
        BookingReference = bookingReference;
        ServiceId = serviceId;
        ServiceName = serviceName;
        StaffId = staffId;
        ResourceName = resourceName;
        ScheduledDate = scheduledDate;
        ScheduledStartTime = scheduledStartTime;
        ScheduledEndTime = scheduledEndTime;
        DurationMinutes = durationMinutes;
        ConfirmationMode = confirmationMode;
        PriceSnapshot = priceSnapshot;
        CurrencyCode = currencyCode;
        CustomerNotes = customerNotes;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Booking Create(
        TenantId tenantId,
        string bookingReference,
        ServiceId serviceId,
        string serviceName,
        StaffId staffId,
        string staffName,
        string guestFirstName,
        string guestLastName,
        string guestEmail,
        string? guestPhone,
        DateOnly scheduledDate,
        TimeOnly scheduledStartTime,
        int durationMinutes,
        BookingConfirmationMode confirmationMode,
        decimal priceSnapshot,
        string currencyCode,
        string? customerNotes = null)
    {
        if (tenantId is null)
            throw new BusinessRuleBrokenException("Tenant is required.");

        if (string.IsNullOrWhiteSpace(bookingReference))
            throw new BusinessRuleBrokenException("Booking reference is required.");

        if (serviceId is null)
            throw new BusinessRuleBrokenException("Service is required.");

        if (staffId is null)
            throw new BusinessRuleBrokenException("Staff is required.");

        if (durationMinutes <= 0)
            throw new BusinessRuleBrokenException("Duration must be greater than zero.");

        if (priceSnapshot < 0)
            throw new BusinessRuleBrokenException("Price cannot be negative.");

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            throw new BusinessRuleBrokenException("A valid ISO 4217 currency code is required.");

        var scheduledEndTime = scheduledStartTime.Add(TimeSpan.FromMinutes(durationMinutes));

        var booking = new Booking(
            tenantId,
            bookingReference,
            serviceId,
            serviceName,
            staffId,
            staffName,
            scheduledDate,
            scheduledStartTime,
            scheduledEndTime,
            durationMinutes,
            confirmationMode,
            priceSnapshot,
            currencyCode.ToUpperInvariant(),
            customerNotes);

        // Set initial status based on price and confirmation mode.
        // TR-10.1 Step 7 and Step 8
        if (priceSnapshot > 0)
        {
            booking.Status = BookingStatus.PendingPayment;
        }
        else
        {
            booking.Status = confirmationMode == BookingConfirmationMode.Automatic
                ? BookingStatus.Confirmed
                : BookingStatus.Pending;
        }

        booking.Guest = Guest.Create(tenantId, booking.BookingId, guestFirstName, guestLastName, guestEmail, guestPhone);

        booking._statusLogs.Add(BookingStatusLog.Create(
            booking.BookingId,
            booking.TenantId,
            previousStatus: null,
            newStatus: booking.Status,
            actorType: BookingActor.GuestCustomer,
            reason: "Booking created."));

        return booking;
    }

    public void Confirm(Guid confirmedByUserId)
    {
        if (Status != BookingStatus.Pending && Status != BookingStatus.PendingPayment)
            throw new BusinessRuleBrokenException("Only pending bookings can be confirmed.");

        var previous = Status;
        Status = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        _statusLogs.Add(BookingStatusLog.Create(
            BookingId, TenantId, previous, Status, BookingActor.TenantAdmin, "Booking confirmed."));
    }

    public void Cancel(Guid cancelledByUserId, string? reason = null)
    {
        if (Status == BookingStatus.Cancelled)
            throw new BusinessRuleBrokenException("Booking is already cancelled.");

        if (Status == BookingStatus.Completed)
            throw new BusinessRuleBrokenException("Completed bookings cannot be cancelled.");

        if (Status == BookingStatus.NoShow)
            throw new BusinessRuleBrokenException("No-show bookings cannot be cancelled.");

        var previous = Status;
        Status = BookingStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        CancelledByUserId = cancelledByUserId;
        UpdatedAt = DateTime.UtcNow;

        _statusLogs.Add(BookingStatusLog.Create(
            BookingId, TenantId, previous, Status, BookingActor.TenantAdmin, reason ?? "Booking cancelled."));
    }

    public void GuestCancel(string? reason = null)
    {
        if (Status == BookingStatus.Cancelled)
            throw new BusinessRuleBrokenException("Booking is already cancelled.");

        if (Status == BookingStatus.Completed)
            throw new BusinessRuleBrokenException("Completed bookings cannot be cancelled.");

        if (Status == BookingStatus.NoShow)
            throw new BusinessRuleBrokenException("No-show bookings cannot be cancelled.");

        var previous = Status;
        Status = BookingStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
        CancelledByUserId = null;
        UpdatedAt = DateTime.UtcNow;

        _statusLogs.Add(BookingStatusLog.Create(
            BookingId, TenantId, previous, Status, BookingActor.GuestCustomer, reason ?? "Cancelled by guest."));
    }

    public void MarkAsCompleted(Guid completedByUserId)
    {
        if (Status != BookingStatus.Confirmed)
            throw new BusinessRuleBrokenException("Only confirmed bookings can be marked as completed.");

        var previous = Status;
        Status = BookingStatus.Completed;
        UpdatedAt = DateTime.UtcNow;

        _statusLogs.Add(BookingStatusLog.Create(
            BookingId, TenantId, previous, Status, BookingActor.TenantAdmin, "Booking completed."));
    }

    public void MarkAsNoShow(Guid markedByUserId)
    {
        if (Status != BookingStatus.Confirmed)
            throw new BusinessRuleBrokenException("Only confirmed bookings can be marked as no-show.");

        var previous = Status;
        Status = BookingStatus.NoShow;
        UpdatedAt = DateTime.UtcNow;

        _statusLogs.Add(BookingStatusLog.Create(
            BookingId, TenantId, previous, Status, BookingActor.TenantAdmin, "Customer did not show up."));
    }

    public void MarkPaymentCaptured()
    {
        if (Status != BookingStatus.PendingPayment)
            throw new BusinessRuleBrokenException("Booking is not awaiting payment.");

        var previous = Status;
        Status = ConfirmationMode == BookingConfirmationMode.Automatic
            ? BookingStatus.Confirmed
            : BookingStatus.Pending;

        UpdatedAt = DateTime.UtcNow;

        _statusLogs.Add(BookingStatusLog.Create(
            BookingId, TenantId, previous, Status, BookingActor.PaymentGateway, "Payment captured."));
    }
}
