// Mirrors ApexBooking.Core.Domain.Enums.BookingStatus (wire value = enum.ToString())
export const BookingStatus = {
  PendingPayment: 'PendingPayment',
  Scheduled: 'Scheduled',
  Completed: 'Completed',
  NoShow: 'NoShow',
  Cancelled: 'Cancelled',
} as const

export type BookingStatus = (typeof BookingStatus)[keyof typeof BookingStatus]
