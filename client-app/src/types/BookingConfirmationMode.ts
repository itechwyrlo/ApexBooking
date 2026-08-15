export const BookingConfirmationMode = {
  Automatic: 'Automatic',
  Manual: 'Manual',
} as const

export type BookingConfirmationMode = (typeof BookingConfirmationMode)[keyof typeof BookingConfirmationMode]
