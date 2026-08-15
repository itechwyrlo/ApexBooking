// Mirrors ApexBooking.Core.Domain.Services.BookingEngine.AvailableSlotResponse
export interface IPublicSlot {
  timeString: string // display value, e.g. "09:00 AM"
  rawTime: string // "HH:mm:ss" — submitted as scheduledStartTime
}

// Mirrors ApexBooking.Core.Application.Features.Bookings.Queries.Slots.AvailableSlotsResult
export interface IAvailableSlotsResult {
  slots: IPublicSlot[]
  unavailableReason: string | null
}
