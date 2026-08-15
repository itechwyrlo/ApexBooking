// Mirrors ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicBranches.BranchSummary
export interface IPublicBranch {
  branchId: string
  branchName: string
  street: string
  barangay: string | null
  city: string
  province: string
  zipCode: string
  timeZoneId: string
}
