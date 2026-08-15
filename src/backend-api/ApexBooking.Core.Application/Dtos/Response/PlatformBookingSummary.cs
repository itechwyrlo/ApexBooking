namespace ApexBooking.Core.Application.Dtos.Response
{
    public record PlatformBookingSummary(
        Guid BookingId,
        string BookingReference,
        Guid TenantId,
        string TenantName,
        string CustomerName,
        string StaffName,
        string ServiceName,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        string Status
    );
}
