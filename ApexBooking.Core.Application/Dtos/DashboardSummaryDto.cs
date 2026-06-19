namespace ApexBooking.Core.Application.Dtos
{
    public sealed record DashboardSummaryDto(
        int TodayBookingCount,
        int PendingConfirmationCount,
        decimal RevenueToday,
        int TotalBookingCount,
        IReadOnlyList<DailyRevenueDto> WeeklyRevenue,
        IReadOnlyList<ServiceBreakdownDto> ServiceBreakdown,
        IReadOnlyList<ScheduleItemDto> TodaySchedule
    );

    public sealed record DailyRevenueDto(string Date, string DayName, decimal Revenue);

    public sealed record ServiceBreakdownDto(string ServiceName, int BookingCount, double Percentage);

    public sealed record ScheduleItemDto(
        string GuestName,
        string ServiceName,
        string StaffName,
        string ScheduledStartTime,
        string ScheduledEndTime,
        string Status
    );
}
