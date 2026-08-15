import { authClient } from '../api/clients/authClient'
import type { IBusinessProfile, IBusinessProfileValues } from '../interfaces/IBusinessProfile'
import type { BusinessType } from '../types/BusinessType'

// Raw wire shape from ApexBooking.Core.Application.Features.Tenancy.Queries.GetBusinessProfile.BusinessProfileDto
interface IBusinessProfileWire {
  businessName: string
  description: string | null
  logoUrl: string | null
  businessType: BusinessType
  contactPhoneNumber: string | null
}

function toBusinessProfile(wire: IBusinessProfileWire): IBusinessProfile {
  return {
    businessName: wire.businessName,
    description: wire.description,
    businessType: wire.businessType,
    contactPhoneNumber: wire.contactPhoneNumber,
  }
}

export async function getBusinessProfile(): Promise<IBusinessProfile> {
  const response = await authClient.get<IBusinessProfileWire>('/api/Tenant/profile')
  return toBusinessProfile(response.data)
}

export async function updateBusinessProfile(values: IBusinessProfileValues): Promise<void> {
  await authClient.put('/api/Tenant/profile', {
    businessName: values.businessName,
    description: values.description || null,
    logoUrl: null,
    contactPhoneNumber: values.contactPhoneNumber || null,
  })
}
