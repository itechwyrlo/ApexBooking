// namespace ApexBooking.Core.Domain.Scheduling;

// /// <summary>
// /// The scheduling engine (06 §1). Pure, stateless time geometry: given a fully-assembled
// /// <see cref="SchedulingRequest"/>, returns the bookable slots. Lives entirely in the Domain layer —
// /// interface and implementation — because it touches no repository and no DbContext. Advisory only;
// /// the commit path still enforces double-booking (05 §6 pre-check + RowVersion).
// /// </summary>
// public interface ISchedulingService
// {
//     IReadOnlyList<AvailableSlot> GetAvailableSlots(SchedulingRequest request);
// }
