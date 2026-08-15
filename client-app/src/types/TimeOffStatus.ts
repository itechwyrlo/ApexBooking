// Mirrors ApexBooking.Core.Domain.Enums.TimeOffStatus (wire value = enum.ToString())
export const TimeOffStatus = {
  Requested: 'Requested',
  Approved: 'Approved',
  Rejected: 'Rejected',
} as const

export type TimeOffStatus = (typeof TimeOffStatus)[keyof typeof TimeOffStatus]
