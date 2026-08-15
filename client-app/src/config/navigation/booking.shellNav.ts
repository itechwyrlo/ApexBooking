import { buildDashboardPath } from '../dashboardRoutes'
import { NAV_SECTION_LABELS } from './booking.nav.config'
import type { ISidebarNavItem, NavSection } from '../../interfaces/ISidebarNavItem'
import type { IShellNavSection } from '../../interfaces/IShellNavItem'

const SECTION_ORDER: NavSection[] = ['scheduling', 'manage', 'settings']

// `visibleItems` must already be permission-filtered by the caller (usePermissions().hasAccess) —
// this only groups the already-allowed items and resolves their hrefs; it makes no access
// decisions of its own, so role access is exactly as it was before this refactor.
export function buildBookingNavSections(visibleItems: ISidebarNavItem[], slug: string): IShellNavSection[] {
  return SECTION_ORDER.map((section) => ({
    key: section,
    label: section !== 'settings' ? NAV_SECTION_LABELS[section] : undefined,
    showDividerBefore: section === 'settings',
    items: visibleItems
      .filter((item) => item.section === section)
      .map((item) => ({
        key: item.href,
        label: item.label,
        icon: item.icon,
        href: buildDashboardPath(slug, item.href),
        end: item.href === '',
      })),
  })).filter((section) => section.items.length > 0)
}
