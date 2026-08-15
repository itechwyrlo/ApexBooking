import type { DayOfWeek } from '../types/DayOfWeek'

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetBranch.OperatingHoursEntryDto
// TimeOnly values serialize as "HH:mm:ss" strings.
export interface IOperatingHoursEntry {
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  isOff: boolean
}
