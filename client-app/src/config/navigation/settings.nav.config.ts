export interface ISettingsNavItem {
  label: string
  href: string
}

// href values are relative to the current tenant's dashboard root, same convention as
// booking.nav.config.ts — resolved at render time in SettingsLayout.tsx.
export const SETTINGS_NAV_ITEMS: ISettingsNavItem[] = [
  { label: 'Booking Settings', href: 'settings/booking' },
  { label: 'Payment Settings', href: 'settings/payment' },
  { label: 'Appearance', href: 'settings/appearance' },
]
