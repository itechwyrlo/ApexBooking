import type { SubscriptionPlanType } from '../types/SubscriptionPlanType'

export interface IPricingPlan {
  id: string
  name: SubscriptionPlanType
  description: string
  features: string[]
  ctaLabel: string
  recommended?: boolean
}
