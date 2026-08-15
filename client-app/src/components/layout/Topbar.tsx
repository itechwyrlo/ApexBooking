import { Link } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { Icon } from '../common/Icon'
import { Button } from '../common/Button'
import { NotificationBell } from '../common/NotificationBell'
import { InstallAppButton } from '../pwa/InstallAppButton'

interface ITopbarProps {
  onMenuClick: () => void
  isSidebarCollapsed?: boolean
  onToggleSidebar?: () => void
  /** Super Admin's static "Platform Administration" label next to the sidebar toggle. Tenant has none. */
  titleLabel?: string
  /** Tenant-only: the PWA install prompt — only ever offered from inside an authenticated workspace shell. */
  showInstallButton?: boolean
  /** Hidden entirely on the Basic plan — dark mode is a Professional+ capability. Tenant-only. */
  showDarkModeToggle?: boolean
  isDarkMode?: boolean
  onToggleDarkMode?: () => void
  /** Tenant-only: link to the public booking page. */
  publicBookingHref?: string
  /** Secondary line under the email in the account-menu header (the user's role list for tenant, "Platform Administrator" for admin). */
  accountMenuSubtitle?: string | null
  /** Tenant-only: Settings link in the account menu. */
  settingsHref?: string
}

// Shared, config-driven topbar for both the tenant dashboard shell and the Super Admin shell —
// every role-specific control (install prompt, dark mode, public-booking link, Settings link,
// title label) is prop-gated rather than forked into a second component
// (components/admin/AdminTopbar.tsx, now retired). Every rendered piece here already existed in
// one or the other original file — nothing new was added.
export function Topbar({
  onMenuClick,
  isSidebarCollapsed,
  onToggleSidebar,
  titleLabel,
  showInstallButton,
  showDarkModeToggle,
  isDarkMode,
  onToggleDarkMode,
  publicBookingHref,
  accountMenuSubtitle,
  settingsHref,
}: ITopbarProps) {
  const { user, logout } = useAuth()
  const initial = user?.email?.charAt(0).toUpperCase() ?? '?'

  return (
    <header className="d-flex align-items-center border-bottom bg-white px-3 px-md-4 py-2 py-md-3">
      <Button
        variant="outline-secondary"
        className="d-lg-none"
        icon="menu"
        iconOnly
        aria-label="Open menu"
        onClick={onMenuClick}
      >
        Open menu
      </Button>
      <Button
        variant="outline-secondary"
        className="d-none d-lg-inline-flex"
        icon="panel-left"
        iconOnly
        aria-label={isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        aria-pressed={isSidebarCollapsed}
        onClick={onToggleSidebar}
      >
        {isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
      </Button>
      {titleLabel && (
        <span className="ms-2 ms-lg-3 fw-semibold text-muted small text-uppercase" style={{ letterSpacing: '0.06em' }}>
          {titleLabel}
        </span>
      )}
      <div className="ms-auto d-flex align-items-center gap-3">
        {showInstallButton && <InstallAppButton />}
        {showDarkModeToggle && (
          <Button
            variant="outline-secondary"
            icon={isDarkMode ? 'sun' : 'moon'}
            iconOnly
            aria-label={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
            onClick={onToggleDarkMode}
          >
            {isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
          </Button>
        )}
        {publicBookingHref && (
          <a
            href={publicBookingHref}
            target="_blank"
            rel="noopener noreferrer"
            className="btn btn-outline-secondary btn-icon"
            title="View public booking page"
            aria-label="View public booking page"
          >
            <Icon name="globe" size={16} />
          </a>
        )}
        <NotificationBell />
        <div className="dropdown">
          <button
            type="button"
            className="btn btn-light d-flex align-items-center gap-1 border-0 px-2"
            data-bs-toggle="dropdown"
            aria-expanded="false"
            aria-label="Account menu"
          >
            <span
              className="d-inline-flex align-items-center justify-content-center rounded-circle bg-primary text-white fw-semibold"
              style={{ width: 32, height: 32, fontSize: '0.85rem' }}
              aria-hidden="true"
            >
              {initial}
            </span>
            <Icon name="chevron-down" size={14} />
          </button>
          <ul className="dropdown-menu dropdown-menu-end shadow">
            <li className="dropdown-header">
              <div className="text-truncate text-body fw-semibold" style={{ maxWidth: 220 }}>
                {user?.email}
              </div>
              {accountMenuSubtitle && <div className="text-muted small">{accountMenuSubtitle}</div>}
            </li>
            <li>
              <hr className="dropdown-divider" />
            </li>
            {settingsHref && (
              <>
                <li>
                  <Link className="dropdown-item d-flex align-items-center gap-2" to={settingsHref}>
                    <Icon name="settings" size={16} />
                    Settings
                  </Link>
                </li>
                <li>
                  <hr className="dropdown-divider" />
                </li>
              </>
            )}
            <li>
              <button
                type="button"
                className="dropdown-item d-flex align-items-center gap-2 text-danger"
                onClick={() => void logout()}
              >
                <Icon name="log-out" size={16} />
                Sign Out
              </button>
            </li>
          </ul>
        </div>
      </div>
    </header>
  )
}
