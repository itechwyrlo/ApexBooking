namespace ApexBooking.Core.Application.Dtos.Response
{
    public record PlatformDashboardSummary(
        int TotalTenants,
        int ActiveTenants,
        int PendingRequests,
        int ApprovedRequests,
        int RejectedRequests,
        int BookingsToday,
        int BookingsThisMonth
    );
}
