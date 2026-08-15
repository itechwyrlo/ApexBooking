export const SubscriptionPlanType = {
  Basic: 'Basic',
  Professional: 'Professional',
  Enterprise: 'Enterprise',
} as const

export type SubscriptionPlanType = (typeof SubscriptionPlanType)[keyof typeof SubscriptionPlanType]
