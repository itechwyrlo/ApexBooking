// Mirrors ApexBooking.Core.Application.Features.Services.Queries.GetServiceStaff.StaffAssignmentSummary
export interface IStaffAssignment {
  tenantMemberId: string
  fullName: string
  customJobTitle: string | null
  isAssigned: boolean
}
