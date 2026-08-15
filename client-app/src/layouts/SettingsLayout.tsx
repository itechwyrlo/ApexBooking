import { NavLink, Outlet } from 'react-router-dom'
import { SETTINGS_NAV_ITEMS } from '../config/navigation/settings.nav.config'
import { PageHeader } from '../components/common/PageHeader'
import { buildDashboardPath } from '../config/dashboardRoutes'
import { useAuth } from '../hooks/useAuth'

export function SettingsLayout() {
  const { user } = useAuth()
  const slug = user?.slug ?? ''

  return (
    <div>
      <PageHeader
        title="Settings"
        description="Configure how your booking business runs."
        breadcrumbs={[{ label: 'Dashboard', href: buildDashboardPath(slug) }, { label: 'Settings' }]}
      />
      <nav className="tabs-scroll d-md-none mb-4" aria-label="Settings sections">
        {SETTINGS_NAV_ITEMS.map((item) => (
          <NavLink
            key={item.href}
            to={buildDashboardPath(slug, item.href)}
            className={({ isActive }) => `tab-link${isActive ? ' active' : ''}`}
          >
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="row g-4">
        <div className="col-md-3 d-none d-md-block">
          <ul className="nav flex-column gap-1 list-unstyled">
            {SETTINGS_NAV_ITEMS.map((item) => (
              <li key={item.href}>
                <NavLink
                  to={buildDashboardPath(slug, item.href)}
                  className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}
                >
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </div>
        <div className="col-12 col-md-9">
          <Outlet />
        </div>
      </div>
    </div>
  )
}
