// Mirrors ApexBooking.Core.Domain.Enums.TimeOffType (wire value = enum.ToString())
export const TimeOffType = {
  FullDay: 'FullDay',
  PartialDay: 'PartialDay',
} as const

export type TimeOffType = (typeof TimeOffType)[keyof typeof TimeOffType]
