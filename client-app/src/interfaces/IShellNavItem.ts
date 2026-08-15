// Shared, already-resolved nav-item shape the application shell renders — both the tenant
// (BOOKING_NAV_ITEMS, slug-relative hrefs + role filtering) and Super Admin (ADMIN_NAV_ITEMS,
// absolute hrefs, no role filtering) navigation configs are adapted into this shape by their own
// small adapter (booking.shellNav.ts / admin.shellNav.ts) *after* the exact same permission
// filtering each already did — the shell itself never re-derives access, only renders what it's
// given.
export interface IShellNavItem {
  /** Unique within its section — the resolved href works for this. */
  key: string
  label: string
  /** Path to a monochrome icon asset, e.g. "/assets/icons/dashboard.svg" (Icon.tsx's convention). */
  icon: string
  /** Fully resolved app path — already slug-prefixed for tenant items, already absolute for admin items. */
  href: string
  /** NavLink's `end` prop — true only for an item whose href is a prefix of a sibling's (e.g. the dashboard root). */
  end?: boolean
}

export interface IShellNavSection {
  /** Stable identifier — also what BottomNav's `primarySectionKey` matches against. */
  key: string
  /** Omitted for an ungrouped flat list (Super Admin has no sections today). */
  label?: string
  /** Renders a divider above this section (mirrors today's Sidebar "settings" section treatment). */
  showDividerBefore?: boolean
  items: IShellNavItem[]
}
