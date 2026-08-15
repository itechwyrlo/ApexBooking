import type { IBranch } from './IBranch'
import type { IOperatingHoursEntry } from './IOperatingHoursEntry'

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetBranch.BranchDetailDto
export interface IBranchDetail extends IBranch {
  operatingHours: IOperatingHoursEntry[]
}

export interface IBranchFormValues {
  branchName: string
  timeZoneId: string
  street: string
  barangay: string
  city: string
  province: string
  zipCode: string
}
