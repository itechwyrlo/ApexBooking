import type { IShellNavItem, IShellNavSection } from '../interfaces/IShellNavItem'

const MAX_PRIMARY_ITEMS = 4

export interface IBottomNavPlan {
  primary: IShellNavItem[]
  hasMore: boolean
}

// Picks the mobile bottom bar's primary destinations. When `primarySectionKey` names a real
// section (tenant: the existing "scheduling" section), that section's items are the primary
// candidates — the app's own nav config already separates day-to-day scheduling operations
// (Dashboard, Appointments, Calendar, Time Offs) from administrative "manage"/"settings" items,
// so Step 2 reuses that existing distinction instead of inventing a new one. Everything in every
// other section (plus any overflow within the primary section itself) goes to "More".
//
// Without a key (Super Admin's flat, single-section config), every item is a candidate and
// whatever doesn't fit within MAX_PRIMARY_ITEMS overflows — so a role only gets a "More" entry
// when something is actually omitted (Staff and Super Admin currently have exactly 4 items total
// each, so both end up with no "More" at all).
export function deriveBottomNav(sections: IShellNavSection[], primarySectionKey?: string): IBottomNavPlan {
  const primarySection = primarySectionKey ? sections.find((section) => section.key === primarySectionKey) : undefined

  const candidateItems = primarySection ? primarySection.items : sections.flatMap((section) => section.items)
  const totalItemCount = sections.reduce((count, section) => count + section.items.length, 0)

  const primary = candidateItems.slice(0, MAX_PRIMARY_ITEMS)
  const hasMore = totalItemCount > primary.length

  return { primary, hasMore }
}
