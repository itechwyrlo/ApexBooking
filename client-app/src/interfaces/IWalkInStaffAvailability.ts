// Mirrors ApexBooking.Core.Application.Dtos.Response.WalkInTimeOption
export interface IWalkInTimeOption {
  display: string
  raw: string
}

// Mirrors ApexBooking.Core.Application.Dtos.Response.WalkInStaffAvailability
export interface IWalkInStaffAvailability {
  tenantMemberId: string
  fullName: string
  customJobTitle: string | null
  photoUrl: string | null
  isAvailableNow: boolean
  recommendedTimeDisplay: string | null
  recommendedTimeRaw: string | null
  availableUntilDisplay: string | null
  alternateTimes: IWalkInTimeOption[]
  unavailableReason: string | null
}
