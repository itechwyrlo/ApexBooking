// using ApexBooking.Core.Domain.Enums;

// namespace ApexBooking.Core.Domain.Scheduling;
// public sealed class SchedulingService : ISchedulingService
// {
//     public IReadOnlyList<AvailableSlot> GetAvailableSlots(SchedulingRequest request)
//     {
//         if (request.RequestedServices.Count == 0 || request.TotalOccupiedMinutes <= 0)
//             return [];

//         // 1. Business window for the date (special hours override operating hours).
//         var businessWindow = ResolveBusinessWindow(request);
//         if (businessWindow is null)
//             return [];

//         var (bizOpen, bizClose) = businessWindow.Value;
//         var occupied = TimeSpan.FromMinutes(request.TotalOccupiedMinutes);
//         var results = new List<AvailableSlot>();

//         foreach (var staff in request.CandidateStaff)
//         {
//             // 2. Staff working window ∩ business window.
//             if (staff.IsOnFullDayTimeOff(request.Date))
//                 continue;

//             var schedule = staff.GetDaySchedule(request.Date.DayOfWeek);
//             if (schedule is null || schedule.IsOff || schedule.StartTime >= schedule.EndTime)
//                 continue;

//             var effOpen = Max(bizOpen, schedule.StartTime);
//             var effClose = Min(bizClose, schedule.EndTime);
//             if (effOpen >= effClose)
//                 continue;

//             // 3. Blocked intervals for this staff: breaks, approved partial time off, existing bookings.
//             var blocked = new List<(TimeOnly Start, TimeOnly End)>();
//             foreach (var brk in staff.Breaks)
//                 blocked.Add((brk.StartTime, brk.EndTime));
//             foreach (var window in PartialTimeOffWindows(staff, request.Date))
//                 blocked.Add(window);
//             foreach (var booking in request.ExistingBookings.Where(b => b.StaffId == staff.StaffId))
//                 blocked.Add((booking.ScheduledStartTime, booking.ScheduledEndTime));

//             // 4. Walk candidate starts by SlotInterval; a slot fits if [start, end) is within the
//             //    effective window, not in the past, and overlaps no blocked interval.
//             var open = effOpen.ToTimeSpan();
//             var close = effClose.ToTimeSpan();
//             for (var cursor = open; cursor + occupied <= close; cursor += request.SlotInterval)
//             {
//                 var start = TimeOnly.FromTimeSpan(cursor);
//                 var end = TimeOnly.FromTimeSpan(cursor + occupied);

//                 var startDateTime = request.Date.ToDateTime(start);
//                 if (startDateTime < request.Now)
//                     continue;

//                 var overlaps = blocked.Any(b => start < b.End && b.Start < end);
//                 if (overlaps)
//                     continue;

//                 results.Add(new AvailableSlot(startDateTime, request.Date.ToDateTime(end), staff.StaffId));
//             }
//         }

//         return results
//             .OrderBy(s => s.Start)
//             .ThenBy(s => s.StaffId.Value)
//             .ToList();
//     }

//     private static (TimeOnly Open, TimeOnly Close)? ResolveBusinessWindow(SchedulingRequest request)
//     {
//         if (request.SpecialHours is not null)
//         {
//             if (request.SpecialHours.IsClosed)
//                 return null;
//             if (request.SpecialHours.SpecialOpenTime is TimeOnly open &&
//                 request.SpecialHours.SpecialCloseTime is TimeOnly close &&
//                 open < close)
//                 return (open, close);
//             return null;
//         }

//         if (request.OperatingHours is null ||
//             request.OperatingHours.IsClosed ||
//             request.OperatingHours.OpenTime >= request.OperatingHours.CloseTime)
//             return null;

//         return (request.OperatingHours.OpenTime, request.OperatingHours.CloseTime);
//     }

//     private static IEnumerable<(TimeOnly Start, TimeOnly End)> PartialTimeOffWindows(Staff staff, DateOnly date) =>
//         staff.TimeOffRequests
//             .Where(r => r.Status == TimeOffStatus.Approved
//                         && r.Type == TimeOffType.PartialDay
//                         && date >= r.StartDate && date <= r.EndDate
//                         && r.StartTime is not null && r.EndTime is not null)
//             .Select(r => (r.StartTime!.Value, r.EndTime!.Value));

//     private static TimeOnly Max(TimeOnly a, TimeOnly b) => a > b ? a : b;
//     private static TimeOnly Min(TimeOnly a, TimeOnly b) => a < b ? a : b;
// }
