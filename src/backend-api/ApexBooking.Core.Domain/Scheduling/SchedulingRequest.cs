// using ApexBooking.Core.Domain.Entities;

// namespace ApexBooking.Core.Domain.Scheduling;

// /// <summary>
// /// The fully-assembled input to <see cref="ISchedulingService"/> (06 §1). The Application handler
// /// gathers every fact — business hours, candidate staff (loaded with their schedule/breaks/time off),
// /// existing bookings — and hands them in. The service fetches nothing; it does time geometry over
// /// what it is given.
// /// </summary>
// public sealed class SchedulingRequest
// {
//     /// <summary>The day being scheduled.</summary>
//     public DateOnly Date { get; }

//     /// <summary>"Now" in the business's local time — used to reject start times already in the past.</summary>
//     public DateTime Now { get; }

//     /// <summary>The requested service(s); each contributes duration + both buffers. Must be Active (handler-checked).</summary>
//     public IReadOnlyList<Service> RequestedServices { get; }

//     /// <summary>Business operating hours for <see cref="Date"/>'s day of week.</summary>
//     public OperatingHoursEntry? OperatingHours { get; }

//     /// <summary>Special hours entry for <see cref="Date"/>, if any — overrides <see cref="OperatingHours"/>.</summary>
//     public SpecialHoursEntry? SpecialHours { get; }

//     /// <summary>Candidate staff, each loaded with its weekly schedule, breaks, and approved time off.</summary>
//     public IReadOnlyList<Staff> CandidateStaff { get; }

//     /// <summary>Active bookings (Pending/Confirmed/CheckedIn) for the candidate staff on <see cref="Date"/>.</summary>
//     public IReadOnlyList<Booking> ExistingBookings { get; }

//     /// <summary>Granularity of candidate start times (system default for MVP, 06 §Resolved 1).</summary>
//     public TimeSpan SlotInterval { get; }

//     /// <summary>The contiguous block one staff member must be free for: sum(BufferBefore + Duration + BufferAfter).</summary>
//     public int TotalOccupiedMinutes =>
//         RequestedServices.Sum(s => s.DurationMinutes + s.BufferBeforeMinutes + s.BufferAfterMinutes);

//     public SchedulingRequest(
//         DateOnly date,
//         DateTime now,
//         IReadOnlyList<Service> requestedServices,
//         OperatingHoursEntry? operatingHours,
//         SpecialHoursEntry? specialHours,
//         IReadOnlyList<Staff> candidateStaff,
//         IReadOnlyList<Booking> existingBookings,
//         TimeSpan slotInterval)
//     {
//         Date = date;
//         Now = now;
//         RequestedServices = requestedServices ?? [];
//         OperatingHours = operatingHours;
//         SpecialHours = specialHours;
//         CandidateStaff = candidateStaff ?? [];
//         ExistingBookings = existingBookings ?? [];
//         SlotInterval = slotInterval <= TimeSpan.Zero ? TimeSpan.FromMinutes(15) : slotInterval;
//     }
// }
