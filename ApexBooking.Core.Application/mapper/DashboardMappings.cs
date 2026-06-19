using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.mapper
{
    public static class DashboardMappings
    {
        public static DashboardSummaryDto ToDashboardSummaryDto(this IEnumerable<Booking> bookings)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var list = bookings.ToList();

            var todayActive = list
                .Where(b => b.ScheduledDate == today
                    && b.Status != BookingStatus.Cancelled
                    && b.Status != BookingStatus.NoShow)
                .ToList();

            var todayBookingCount = todayActive.Count;

            var pendingConfirmationCount = list.Count(b => b.Status == BookingStatus.Pending);

            var revenueToday = todayActive
                .Where(b => b.Status == BookingStatus.Completed)
                .Sum(b => b.PriceSnapshot);

            var activeList = list
                .Where(b => b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.NoShow)
                .ToList();

            var totalBookingCount = activeList.Count;

            var weekStart = today.AddDays(-6);
            var weeklyRevenue = (IReadOnlyList<DailyRevenueDto>)Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var date = weekStart.AddDays(i);
                    var revenue = list
                        .Where(b => b.ScheduledDate == date && b.Status == BookingStatus.Completed)
                        .Sum(b => b.PriceSnapshot);
                    return new DailyRevenueDto(
                        date.ToString("yyyy-MM-dd"),
                        date.DayOfWeek.ToString(),
                        revenue);
                })
                .ToList()
                .AsReadOnly();

            var totalActive = activeList.Count;
            var serviceBreakdown = (IReadOnlyList<ServiceBreakdownDto>)activeList
                .GroupBy(b => b.ServiceName)
                .Select(g =>
                {
                    var count = g.Count();
                    var pct = totalActive > 0 ? Math.Round((double)count / totalActive * 100.0, 1) : 0.0;
                    return new ServiceBreakdownDto(g.Key, count, pct);
                })
                .OrderByDescending(s => s.BookingCount)
                .ToList()
                .AsReadOnly();

            var todaySchedule = (IReadOnlyList<ScheduleItemDto>)todayActive
                .OrderBy(b => b.ScheduledStartTime)
                .Select(b => new ScheduleItemDto(
                    b.Guest is not null
                        ? $"{b.Guest.FirstName} {b.Guest.LastName}"
                        : string.Empty,
                    b.ServiceName,
                    b.ResourceName,
                    b.ScheduledStartTime.ToString("HH:mm"),
                    b.ScheduledEndTime.ToString("HH:mm"),
                    b.Status.ToString()))
                .ToList()
                .AsReadOnly();

            return new DashboardSummaryDto(
                todayBookingCount,
                pendingConfirmationCount,
                revenueToday,
                totalBookingCount,
                weeklyRevenue,
                serviceBreakdown,
                todaySchedule);
        }
    }
}
