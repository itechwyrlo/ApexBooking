import type { TenantRequestStatus } from '../types/TenantRequestStatus'

// Mirrors ApexBooking.Core.Application.Dtos.Request.PendingTenantRequestSummary
export interface ITenantRequest {
  id: string
  businessName: string
  businessType: string
  requestedSlug: string
  requestedPlan: string
  ownerFirstName: string
  ownerLastName: string
  ownerEmail: string
  status: TenantRequestStatus
  requestedAt: string
}
