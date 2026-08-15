// Mirrors ApexBooking.Core.Application.Dtos.Response.StaffBreakSummary
// TimeOnly values serialize as "HH:mm:ss" strings.
export interface IStaffBreak {
  id: string
  name: string
  startTime: string
  endTime: string
}

export interface IAddStaffBreakValues {
  name: string
  startTime: string
  endTime: string
}
