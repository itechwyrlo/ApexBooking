// Mirrors ApexBooking.Core.Application.Dtos.Response.PlatformDashboardSummary
export interface IPlatformDashboardSummary {
  totalTenants: number
  activeTenants: number
  pendingRequests: number
  approvedRequests: number
  rejectedRequests: number
  bookingsToday: number
  bookingsThisMonth: number
}
