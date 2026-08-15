// Mirrors ApexBooking.Core.Application.Features.Bookings.Commands.InitiateBooking.InitiateBookingCommand
export interface IInitiateBookingValues {
  branchId: string
  staffId: string
  serviceId: string
  scheduledDate: string // "yyyy-MM-dd"
  scheduledStartTime: string // "HH:mm:ss"
  customerFirstName: string
  customerLastName: string
  customerEmail: string
  customerPhone: string
  customerNotes: string | null
}

// The contact-only subset the Confirm step's form collects; the rest of
// IInitiateBookingValues comes from earlier wizard steps.
export interface IBookingContactValues {
  customerFirstName: string
  customerLastName: string
  customerEmail: string
  customerPhone: string
  customerNotes: string
}
