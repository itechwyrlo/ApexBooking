// Mirrors ApexBooking.Core.Domain.Enums.PaymentConfirmationMethod
export const PaymentConfirmationMethod = {
  Online: 'Online',
  PayInVisit: 'PayInVisit',
} as const

export type PaymentConfirmationMethod = (typeof PaymentConfirmationMethod)[keyof typeof PaymentConfirmationMethod]
