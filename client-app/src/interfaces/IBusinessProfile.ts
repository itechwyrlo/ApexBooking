import type { BusinessType } from '../types/BusinessType'

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetBusinessProfile.BusinessProfileDto
export interface IBusinessProfile {
  businessName: string
  description: string | null
  businessType: BusinessType
  contactPhoneNumber: string | null
}

export interface IBusinessProfileValues {
  businessName: string
  description: string
  contactPhoneNumber: string
}
