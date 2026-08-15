// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllBranches.BranchAdminSummary
export interface IBranch {
  id: string
  name: string
  street: string
  barangay: string | null
  city: string
  province: string
  zipCode: string
  timeZoneId: string
  isActive: boolean
}
