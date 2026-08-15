import type { SubscriptionPlanType } from '../types/SubscriptionPlanType'

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetAppearance.AppearanceDto
export interface IAppearance {
  themePaletteId: string
  publicPageDarkMode: boolean
  plan: SubscriptionPlanType
}

export interface IAppearanceValues {
  themePaletteId: string
  publicPageDarkMode: boolean
}
