import type { IPricingPlan } from '../interfaces/IPricingPlan'

export const PRICING_PLANS: IPricingPlan[] = [
  {
    id: 'basic',
    name: 'Basic',
    description: 'Designed for small businesses.',
    features: [
      'Booking Calendar',
      'Team Management',
      'Service Management',
      'Customer Booking Page',
      'Email Notifications',
    ],
    ctaLabel: 'Request Access',
  },
  {
    id: 'professional',
    name: 'Professional',
    description: 'Everything in Basic, plus advanced tools for growing businesses.',
    features: [
      'Everything in Basic',
      'Dashboard Reports',
      'Advanced Analytics',
      'Priority Support',
      'SMS Notifications (Coming Soon)',
      'Future Business Modules',
    ],
    ctaLabel: 'Request Access',
    recommended: true,
  },
]
