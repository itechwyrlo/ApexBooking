import { useEffect, useState } from 'react'
import { AppShell } from '../components/layout/AppShell'
import { Topbar } from '../components/layout/Topbar'
import { ModuleSwitcher } from '../components/layout/ModuleSwitcher'
import { useAppearance } from '../hooks/useAppearance'
import { useAuth } from '../hooks/useAuth'
import { usePermissions } from '../hooks/usePermissions'
import { useRefundReviewReminder } from '../hooks/useRefundReviewReminder'
import { RefundReviewReminderBanner } from '../components/refunds/RefundReviewReminderBanner'
import { BOOKING_NAV_ITEMS } from '../config/navigation/booking.nav.config'
import { buildBookingNavSections } from '../config/navigation/booking.shellNav'
import { buildDashboardPath, buildPublicBookingPath } from '../config/dashboardRoutes'
import { SubscriptionPlanType } from '../types/SubscriptionPlanType'
import { Role } from '../types/Role'

const DASHBOARD_DARK_MODE_STORAGE_KEY = 'apexbooking.dashboard.darkMode'
const DASHBOARD_PALETTE_STORAGE_KEY = 'apexbooking.dashboard.paletteId'

export function DashboardLayout() {
  const [isDarkMode, setIsDarkMode] = useState(() => localStorage.getItem(DASHBOARD_DARK_MODE_STORAGE_KEY) === 'true')
  const { appearance } = useAppearance()
  const { user } = useAuth()
  const { hasAccess } = usePermissions()
  const canReviewRefunds = user !== null && (user.roles.includes(Role.Owner) || user.roles.includes(Role.Admin))
  const { dueSoonCount } = useRefundReviewReminder()

  // Personal preference, but gated by the tenant's plan — a Basic-plan tenant's dashboard
  // always renders light, even if a stale preference from a prior Professional plan lingers
  // in this browser (e.g. after a downgrade).
  const canUseDarkMode = appearance !== null && appearance.plan !== SubscriptionPlanType.Basic
  const effectiveDarkMode = canUseDarkMode && isDarkMode

  useEffect(() => {
    document.documentElement.setAttribute('data-bs-theme', effectiveDarkMode ? 'dark' : 'light')
  }, [effectiveDarkMode])

  // Applies a cached palette immediately on mount (before `appearance` has loaded) so a repeat
  // visit doesn't flash the default indigo, then updates it — and the cache — once the real
  // value is fetched.
  useEffect(() => {
    const cachedPaletteId = localStorage.getItem(DASHBOARD_PALETTE_STORAGE_KEY)
    if (cachedPaletteId) {
      document.documentElement.setAttribute('data-palette', cachedPaletteId)
    }
  }, [])

  useEffect(() => {
    if (appearance) {
      document.documentElement.setAttribute('data-palette', appearance.themePaletteId)
      localStorage.setItem(DASHBOARD_PALETTE_STORAGE_KEY, appearance.themePaletteId)
    }
  }, [appearance])

  // Both attributes live on the shared <html> element, so without this, leaving the dashboard
  // (e.g. logging out) would leak dark mode / the tenant's palette into whatever page loads next
  // in the same tab — the login screen, the public booking page, etc.
  useEffect(() => {
    return () => {
      document.documentElement.setAttribute('data-bs-theme', 'light')
      document.documentElement.removeAttribute('data-palette')
    }
  }, [])

  const toggleDarkMode = () => {
    setIsDarkMode((dark) => {
      const next = !dark
      localStorage.setItem(DASHBOARD_DARK_MODE_STORAGE_KEY, String(next))
      return next
    })
  }

  const slug = user?.slug ?? ''
  const visibleItems = BOOKING_NAV_ITEMS.filter(hasAccess)
  const navSections = buildBookingNavSections(visibleItems, slug)
  const roleLabel = user?.roles.length ? user.roles.join(', ') : null

  return (
    <AppShell
      collapseStorageKey="apexbooking.sidebar.collapsed"
      renderBrand={(isCollapsed) => <ModuleSwitcher isCollapsed={isCollapsed} />}
      navSections={navSections}
      bottomNavPrimarySectionKey="scheduling"
      renderTopbar={({ onMenuClick, isSidebarCollapsed, onToggleSidebar }) => (
        <Topbar
          onMenuClick={onMenuClick}
          isSidebarCollapsed={isSidebarCollapsed}
          onToggleSidebar={onToggleSidebar}
          showInstallButton
          showDarkModeToggle={canUseDarkMode}
          isDarkMode={effectiveDarkMode}
          onToggleDarkMode={toggleDarkMode}
          publicBookingHref={buildPublicBookingPath(slug)}
          accountMenuSubtitle={roleLabel}
          settingsHref={buildDashboardPath(slug, 'settings')}
        />
      )}
      topContent={canReviewRefunds && <RefundReviewReminderBanner count={dueSoonCount} />}
    />
  )
}
