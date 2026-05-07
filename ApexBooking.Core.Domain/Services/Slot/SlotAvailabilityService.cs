using System;
using System.Collections.Generic;
using System.Linq;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Domain.Services.Slot
{
    public sealed class SlotAvailabilityService
    {
        public IReadOnlyList<string> ComputeAvailableSlots(
            Service service,
            Resource resource,
            DateOnly date,
            IReadOnlyList<Booking> activeBookings)
        {
            if (service.DurationMinutes <= 0)
                return [];

            var exception = resource.AvailabilityExceptions
                .FirstOrDefault(e => e.ExceptionDate == date);

            if (exception?.ExceptionType == ExceptionType.UnavailableAllDay)
                return [];

            var schedule = resource.GetScheduleForDay(date.DayOfWeek);

            if (schedule is null || !schedule.IsAvailable)
                return [];

            if (schedule.StartTime is null || schedule.EndTime is null)
                return [];

            if (schedule.EndTime <= schedule.StartTime)
                return [];

            var availableRanges = BuildWorkingRanges(schedule);

            if (exception is not null)
            {
                var window = exception.GetTimeWindow();

                if (window is not null)
                {
                    availableRanges = exception.ExceptionType switch
                    {
                        ExceptionType.UnavailableHours =>
                            SubtractWindow(availableRanges, window.Value.Start, window.Value.End),

                        ExceptionType.AvailableExtraHours =>
                            AddWindow(availableRanges, window.Value.Start, window.Value.End),

                        _ => availableRanges
                    };
                }
            }

            if (availableRanges.Count == 0)
                return [];

            availableRanges = NormalizeRanges(availableRanges);

            var blockedWindows = activeBookings
                .Select(b => BuildBlockedWindow(b, service))
                .OrderBy(x => x.Start)
                .ToList();

            var slotDuration = TimeSpan.FromMinutes(service.DurationMinutes);
            var bufferBefore = TimeSpan.FromMinutes(service.BufferBeforeMinutes);
            var bufferAfter = TimeSpan.FromMinutes(service.BufferAfterMinutes);

            var results = new List<string>();

            foreach (var range in availableRanges)
            {
                var cursor = range.Start;

                while (cursor < range.End)
                {
                    var slotStart = cursor;
                    var slotEnd = slotStart.Add(slotDuration);

                    var effectiveStart = slotStart.Add(-bufferBefore);
                    var effectiveEnd = slotEnd.Add(bufferAfter);

                    if (effectiveEnd > range.End)
                        break;

                    if (effectiveStart < range.Start)
                    {
                        cursor = cursor.Add(slotDuration);
                        continue;
                    }

                    var isBlocked = blockedWindows.Any(bw =>
                        effectiveStart < bw.End &&
                        effectiveEnd > bw.Start);

                    if (!isBlocked)
                        results.Add(slotStart.ToString("HH:mm"));

                    cursor = cursor.Add(slotDuration);
                }
            }

            return results
                .Distinct()
                .Order()
                .ToList()
                .AsReadOnly();
        }

        private static List<(TimeOnly Start, TimeOnly End)> BuildWorkingRanges(
            ResourceAvailabilitySchedule schedule)
        {
            var ranges = new List<(TimeOnly Start, TimeOnly End)>
            {
                (schedule.StartTime!.Value, schedule.EndTime!.Value)
            };

            foreach (var breakPeriod in schedule.BreakPeriods)
            {
                ranges = SubtractWindow(
                    ranges,
                    breakPeriod.BreakStartTime,
                    breakPeriod.BreakEndTime);
            }

            return ranges;
        }

        private static List<(TimeOnly Start, TimeOnly End)> SubtractWindow(
            List<(TimeOnly Start, TimeOnly End)> ranges,
            TimeOnly windowStart,
            TimeOnly windowEnd)
        {
            var result = new List<(TimeOnly Start, TimeOnly End)>();

            foreach (var range in ranges)
            {
                if (windowEnd <= range.Start || windowStart >= range.End)
                {
                    result.Add(range);
                    continue;
                }

                if (windowStart > range.Start)
                    result.Add((range.Start, windowStart));

                if (windowEnd < range.End)
                    result.Add((windowEnd, range.End));
            }

            return result;
        }

        private static List<(TimeOnly Start, TimeOnly End)> AddWindow(
            List<(TimeOnly Start, TimeOnly End)> ranges,
            TimeOnly windowStart,
            TimeOnly windowEnd)
        {
            ranges.Add((windowStart, windowEnd));
            return NormalizeRanges(ranges);
        }

        private static List<(TimeOnly Start, TimeOnly End)> NormalizeRanges(
            List<(TimeOnly Start, TimeOnly End)> ranges)
        {
            var sorted = ranges
                .OrderBy(r => r.Start)
                .ToList();

            var merged = new List<(TimeOnly Start, TimeOnly End)>();

            foreach (var range in sorted)
            {
                if (merged.Count == 0 || range.Start > merged[^1].End)
                {
                    merged.Add(range);
                    continue;
                }

                var last = merged[^1];

                merged[^1] = (
                    last.Start,
                    range.End > last.End ? range.End : last.End
                );
            }

            return merged;
        }

        private static (TimeOnly Start, TimeOnly End) BuildBlockedWindow(
            Booking booking,
            Service service)
        {
            var start = booking.ScheduledStartTime
                .Add(-TimeSpan.FromMinutes(service.BufferBeforeMinutes));

            var end = booking.ScheduledEndTime
                .Add(TimeSpan.FromMinutes(service.BufferAfterMinutes));

            return (start, end);
        }
    }
}