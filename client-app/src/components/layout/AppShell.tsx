import { useState, type ReactNode } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from './Sidebar'
import { MobileNav } from './MobileNav'
import { BottomNav } from './BottomNav'
import { Footer } from './Footer'
import { deriveBottomNav } from '../../utils/shellNav'
import type { IShellNavSection } from '../../interfaces/IShellNavItem'

function getInitialCollapsedState(storageKey: string): boolean {
  return localStorage.getItem(storageKey) === 'true'
}

interface IAppShellRenderTopbarContext {
  onMenuClick: () => void
  isSidebarCollapsed: boolean
  onToggleSidebar: () => void
}

interface IAppShellProps {
  /** localStorage key for the desktop sidebar's collapsed/expanded state. Kept distinct per shell
   * instance on purpose — tenant and Super Admin already persisted this separately before this
   * refactor, and merging the two would change behavior (one shared preference for two different
   * account kinds), not just relocate it. */
  collapseStorageKey: string
  renderBrand: (isCollapsed: boolean) => ReactNode
  navSections: IShellNavSection[]
  /** Which section's items become the mobile bottom bar's primary destinations (tenant:
   * "scheduling"). Omit for a flat, single-section config (Super Admin) — see utils/shellNav.ts. */
  bottomNavPrimarySectionKey?: string
  renderTopbar: (ctx: IAppShellRenderTopbarContext) => ReactNode
  /** Rendered above the routed page content, inside <main> — e.g. the tenant's refund-review reminder banner. */
  topContent?: ReactNode
}

// Shared protected-app shell (sidebar + topbar + mobile nav + mobile bottom nav + footer),
// consolidating what were previously two near-identical, independently-maintained
// implementations: DashboardLayout+Sidebar+Topbar (tenant) and
// SuperAdminLayout+AdminSidebar+AdminTopbar (now retired). Navigation content stays entirely
// configuration-driven via `navSections`/`renderBrand`/`renderTopbar` — this component makes no
// role or permission decisions of its own.
export function AppShell({
  collapseStorageKey,
  renderBrand,
  navSections,
  bottomNavPrimarySectionKey,
  renderTopbar,
  topContent,
}: IAppShellProps) {
  const [isMobileNavOpen, setIsMobileNavOpen] = useState(false)
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(() => getInitialCollapsedState(collapseStorageKey))

  const toggleSidebar = () => {
    setIsSidebarCollapsed((collapsed) => {
      const next = !collapsed
      localStorage.setItem(collapseStorageKey, String(next))
      return next
    })
  }

  const { primary: bottomNavItems, hasMore } = deriveBottomNav(navSections, bottomNavPrimarySectionKey)
  const hasBottomNav = bottomNavItems.length > 0

  return (
    <div className="d-flex min-vh-100" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <aside
        className="d-none d-lg-block border-end bg-white flex-shrink-0"
        style={{ width: isSidebarCollapsed ? '76px' : '260px', transition: 'width 0.15s ease' }}
      >
        <Sidebar isCollapsed={isSidebarCollapsed} brand={renderBrand(isSidebarCollapsed)} sections={navSections} />
      </aside>
      <MobileNav isOpen={isMobileNavOpen} onClose={() => setIsMobileNavOpen(false)}>
        <Sidebar brand={renderBrand(false)} sections={navSections} />
      </MobileNav>
      <div
        className={`d-flex flex-column flex-grow-1${hasBottomNav ? ' app-shell-content-with-bottom-nav' : ''}`}
        style={{ minWidth: 0 }}
      >
        {renderTopbar({
          onMenuClick: () => setIsMobileNavOpen(true),
          isSidebarCollapsed,
          onToggleSidebar: toggleSidebar,
        })}
        <main className="flex-grow-1 p-3 p-md-4">
          {topContent}
          <Outlet />
        </main>
        <Footer />
      </div>
      {hasBottomNav && (
        <BottomNav items={bottomNavItems} showMore={hasMore} isMoreOpen={isMobileNavOpen} onMoreClick={() => setIsMobileNavOpen(true)} />
      )}
    </div>
  )
}
