// Mirrors ApexBooking.Core.Application.Dtos.Response.BookableStaffSummary
export interface IPublicStaff {
  tenantMemberId: string
  fullName: string
  customJobTitle: string | null
  photoUrl: string | null
}
