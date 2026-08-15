import type { DayOfWeek } from '../types/DayOfWeek'

// Mirrors ApexBooking.Core.Application.Dtos.Response.DayScheduleSummary /
// ApexBooking.Core.Application.Dtos.Request.DayScheduleUpdateItem
// TimeOnly values serialize as "HH:mm:ss" strings.
export interface IDaySchedule {
  dayOfWeek: DayOfWeek
  startTime: string
  endTime: string
  isOff: boolean
}
