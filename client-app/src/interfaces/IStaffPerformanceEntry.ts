// Mirrors ApexBooking.Core.Application.Dtos.Response.StaffPerformanceEntryDto
export interface IStaffPerformanceEntry {
  tenantMemberId: string
  name: string
  servicesCompleted: number
  revenueGenerated: number
  currencyCode: string
}
