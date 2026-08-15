// Mirrors System.DayOfWeek (Sunday = 0 ... Saturday = 6), serialized as strings via the
// backend's global JsonStringEnumConverter.
export const DayOfWeek = {
  Sunday: 'Sunday',
  Monday: 'Monday',
  Tuesday: 'Tuesday',
  Wednesday: 'Wednesday',
  Thursday: 'Thursday',
  Friday: 'Friday',
  Saturday: 'Saturday',
} as const

export type DayOfWeek = (typeof DayOfWeek)[keyof typeof DayOfWeek]

export const DAYS_OF_WEEK_ORDER: DayOfWeek[] = [
  DayOfWeek.Sunday,
  DayOfWeek.Monday,
  DayOfWeek.Tuesday,
  DayOfWeek.Wednesday,
  DayOfWeek.Thursday,
  DayOfWeek.Friday,
  DayOfWeek.Saturday,
]
