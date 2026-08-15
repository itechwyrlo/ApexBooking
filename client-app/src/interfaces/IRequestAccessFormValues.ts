import type { BusinessType } from '../types/BusinessType'
import type { SubscriptionPlanType } from '../types/SubscriptionPlanType'

export interface IRequestAccessFormValues {
  businessName: string
  businessType: BusinessType | ''
  slug: string
  ownerFirstName: string
  ownerLastName: string
  ownerEmail: string
  requestedPlan: SubscriptionPlanType | ''
}
