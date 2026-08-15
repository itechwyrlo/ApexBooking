// Mirrors ApexBooking.Core.Application.Dtos.Response.BookingInitiationResult
export interface IBookingInitiationResult {
  bookingId: string
  bookingReference: string
  requiresPayment: boolean
  amountToPay: number
  payMongoQrCodeUrl: string | null
  payMongoCheckoutUrl: string | null
  ticketToken: string
  ticketQrCodeDataUri: string
}
