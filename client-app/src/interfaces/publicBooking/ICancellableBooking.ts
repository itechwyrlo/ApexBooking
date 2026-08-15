// Mirrors ApexBooking.Core.Application.Features.PublicBookings.Queries.GetCancellableBooking.CancellableBookingDto
export interface ICancellableBooking {
  bookingReference: string
  serviceName: string
  staffName: string
  branchName: string
  scheduledDate: string
  scheduledStartTime: string
  canCancelOnline: boolean
  unavailableReason: string | null
  isRefundEligible: boolean
}
