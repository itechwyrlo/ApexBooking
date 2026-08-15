import { AppShell } from '../components/layout/AppShell'
import { Topbar } from '../components/layout/Topbar'
import { useAuth } from '../hooks/useAuth'
import { buildAdminNavSections } from '../config/navigation/admin.shellNav'

export function SuperAdminLayout() {
  const { user } = useAuth()
  const navSections = buildAdminNavSections()

  return (
    <AppShell
      collapseStorageKey="apexbooking.adminSidebar.collapsed"
      renderBrand={(isCollapsed) => (
        <div className={`d-flex align-items-center gap-2 px-1${isCollapsed ? ' justify-content-center' : ''}`}>
          <img
            src="/favicon.svg"
            alt={isCollapsed ? 'ApexBooking' : ''}
            aria-hidden={isCollapsed ? undefined : true}
            width={28}
            height={28}
            className="rounded-2 flex-shrink-0"
          />
          {!isCollapsed && (
            <div>
              <p className="fw-bold mb-0 lh-sm">ApexBooking</p>
              <p className="text-muted small mb-0 lh-sm">Super Admin</p>
            </div>
          )}
        </div>
      )}
      navSections={navSections}
      renderTopbar={({ onMenuClick, isSidebarCollapsed, onToggleSidebar }) => (
        <Topbar
          onMenuClick={onMenuClick}
          isSidebarCollapsed={isSidebarCollapsed}
          onToggleSidebar={onToggleSidebar}
          titleLabel="Platform Administration"
          accountMenuSubtitle={user?.isPlatformAdmin ? 'Platform Administrator' : null}
        />
      )}
    />
  )
}
