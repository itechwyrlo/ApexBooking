import React, { useState, useRef, useEffect } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { useLogout } from '../../features/auth/hooks/useLogout';

interface HeaderProps {
  onToggleSidebar: () => void;
}

const PAGE_TITLES: Record<string, string> = {
  '/dashboard': 'Dashboard',
  '/resources': 'Resources',
  '/services': 'Services',
  '/bookings': 'Bookings',
  '/settings': 'Settings',
  '/profile': 'Profile',
};

export const Header: React.FC<HeaderProps> = ({ onToggleSidebar }) => {
  const { user } = useAuth();
  const { logout, isLoading: loggingOut } = useLogout();
  const navigate = useNavigate();
  const { tenant } = useParams<{ tenant: string }>();
  const location = useLocation();

  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);

  const userMenuRef = useRef<HTMLDivElement>(null);
  const notifRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) {
        setUserMenuOpen(false);
      }
      if (notifRef.current && !notifRef.current.contains(e.target as Node)) {
        setNotifOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const pathSuffix = Object.keys(PAGE_TITLES).find(k =>
    location.pathname.endsWith(k)
  );
  const pageTitle = pathSuffix ? PAGE_TITLES[pathSuffix] : 'Dashboard';

  const initials = user?.fullName
    ? user.fullName.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase()
    : user?.email?.[0]?.toUpperCase() ?? '?';

  const displayName = user?.fullName || user?.email || 'User';
  const displayRole = user?.role === 'TenantAdmin' ? 'Admin' : user?.role ?? '';

  const goTo = (path: string) => {
    setUserMenuOpen(false);
    navigate(`/t/${tenant}${path}`);
  };

  return (
    <header
      className="bg-white border-bottom px-4"
      style={{ height: 56, borderColor: 'rgba(0,0,0,0.08)', zIndex: 100 }}
    >
      <div className="d-flex align-items-center justify-content-between h-100">
        {/* Left — hamburger + page title */}
        <div className="d-flex align-items-center gap-3">
          <button
            onClick={onToggleSidebar}
            className="btn btn-link p-0 text-secondary d-flex align-items-center justify-content-center"
            style={{ width: 36, height: 36 }}
          >
            <i className="fas fa-bars" />
          </button>
          <span className="fw-semibold text-dark" style={{ fontSize: 15 }}>{pageTitle}</span>
        </div>

        {/* Right — notifications + user */}
        <div className="d-flex align-items-center gap-2">

          {/* Notification dropdown */}
          <div className="position-relative" ref={notifRef}>
            <button
              className="btn btn-link p-0 text-secondary position-relative d-flex align-items-center justify-content-center"
              style={{ width: 36, height: 36 }}
              onClick={() => { setNotifOpen(p => !p); setUserMenuOpen(false); }}
              aria-label="Notifications"
            >
              <i className="fas fa-bell" style={{ fontSize: 16 }} />
              {/* Unread dot — hidden until there are real notifications */}
              {/* <span className="position-absolute top-0 end-0 badge rounded-pill bg-danger" style={{ fontSize: 9, padding: '2px 4px' }}>3</span> */}
            </button>

            {notifOpen && (
              <div
                className="position-absolute end-0 bg-white border rounded shadow-sm"
                style={{ top: 44, width: 320, zIndex: 200 }}
              >
                <div className="px-3 py-2 border-bottom d-flex align-items-center justify-content-between">
                  <span className="fw-semibold" style={{ fontSize: 14 }}>Notifications</span>
                  <span className="badge bg-secondary" style={{ fontSize: 10 }}>0 new</span>
                </div>
                <div className="p-4 text-center text-muted" style={{ fontSize: 13 }}>
                  <i className="fas fa-bell-slash mb-2 d-block" style={{ fontSize: 24, opacity: 0.3 }} />
                  No notifications yet
                </div>
              </div>
            )}
          </div>

          {/* User dropdown */}
          <div className="position-relative" ref={userMenuRef}>
            <button
              className="btn btn-link p-0 d-flex align-items-center gap-2 text-decoration-none"
              onClick={() => { setUserMenuOpen(p => !p); setNotifOpen(false); }}
            >
              <div
                className="rounded-circle bg-primary d-flex align-items-center justify-content-center text-white fw-bold flex-shrink-0"
                style={{ width: 32, height: 32, fontSize: 13 }}
              >
                {initials}
              </div>
              <div className="d-none d-md-block text-start lh-1">
                <div className="text-dark fw-semibold" style={{ fontSize: 13 }}>{displayName}</div>
                <div className="text-muted" style={{ fontSize: 11 }}>{displayRole}</div>
              </div>
              <i className="fas fa-chevron-down text-muted d-none d-md-block" style={{ fontSize: 10 }} />
            </button>

            {userMenuOpen && (
              <div
                className="position-absolute end-0 bg-white border rounded shadow-sm py-1"
                style={{ top: 44, minWidth: 200, zIndex: 200 }}
              >
                {/* Identity block */}
                <div className="px-3 py-2 border-bottom">
                  <div className="fw-semibold text-dark" style={{ fontSize: 13 }}>{displayName}</div>
                  <div className="text-muted" style={{ fontSize: 12 }}>{user?.email}</div>
                </div>

                <button
                  className="dropdown-item d-flex align-items-center gap-2 px-3 py-2"
                  style={{ fontSize: 14 }}
                  onClick={() => goTo('/profile')}
                >
                  <i className="fas fa-user text-muted" style={{ width: 16 }} />
                  Profile
                </button>

                <button
                  className="dropdown-item d-flex align-items-center gap-2 px-3 py-2"
                  style={{ fontSize: 14 }}
                  onClick={() => goTo('/settings')}
                >
                  <i className="fas fa-cog text-muted" style={{ width: 16 }} />
                  Settings
                </button>

                <div className="border-top my-1" />

                <button
                  className="dropdown-item d-flex align-items-center gap-2 px-3 py-2 text-danger"
                  style={{ fontSize: 14 }}
                  onClick={() => { setUserMenuOpen(false); logout(); }}
                  disabled={loggingOut}
                >
                  <i className="fas fa-sign-out-alt" style={{ width: 16 }} />
                  {loggingOut ? 'Logging out...' : 'Log out'}
                </button>
              </div>
            )}
          </div>

        </div>
      </div>
    </header>
  );
};
