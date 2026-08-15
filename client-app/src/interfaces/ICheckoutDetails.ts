// Mirrors ApexBooking.Core.Application.Dtos.Response.CheckoutDetailsDto
export interface ICheckoutDetails {
  bookingId: string
  bookingReference: string
  customerName: string
  customerEmail: string | null
  customerPhone: string | null
  serviceName: string
  staffName: string
  scheduledDate: string
  scheduledStartTime: string
  amountPaidOnline: number
  remainingBalance: number
  currencyCode: string
}

// Mirrors ApexBooking.Core.Application.Dtos.Response.CheckoutBookingResult
export interface ICheckoutBookingResult {
  bookingId: string
  bookingReference: string
  completedAt: string
  amountSettled: number
  currencyCode: string
}
