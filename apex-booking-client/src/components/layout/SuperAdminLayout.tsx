import React, { useState, useRef, useEffect } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { faTachometerAlt, faBuilding, faCreditCard, faBars, faBell, faBellSlash, faSignOutAlt } from '@fortawesome/free-solid-svg-icons';
import { useLogout } from '../../features/auth/hooks/useLogout';
import { useAuth } from '../../context/AuthContext';
import apexbookingLogo from '../../assets/apexbooking-logo.svg';
import { useNotifications } from '../../features/notifications/hooks/useNotifications';
import { NotificationItem } from '../../features/notifications/components/NotificationItem';

const NAV_ITEMS: { path: string; label: string; icon: IconDefinition; end: boolean }[] = [
  { path: '/superadmin', label: 'Overview', icon: faTachometerAlt, end: true },
  { path: '/superadmin/organizations', label: 'Organizations', icon: faBuilding, end: false },
  { path: '/superadmin/payment-gateway', label: 'Payment Gateway', icon: faCreditCard, end: false },
];

export const SuperAdminLayout: React.FC = () => {
  const { logout, isLoading: logoutLoading } = useLogout();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const notifRef = useRef<HTMLDivElement>(null);
  const { data: notifData, isLoading: notifLoading, isMarkingRead, fetch: fetchNotifications, markAllRead } = useNotifications();

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (notifRef.current && !notifRef.current.contains(e.target as Node)) {
        setNotifOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  return (
    <div className="d-flex min-vh-100 bg-light">
      {/* Sidebar */}
      <aside className={`bg-white border-end d-flex flex-column sa-sidebar${sidebarCollapsed ? ' sa-sidebar-collapsed' : ''}`}>
        {/* Logo / Brand */}
        <div className="border-bottom d-flex align-items-center px-3 sa-sidebar-logo">
          <img
            src={apexbookingLogo}
            alt="ApexBooking"
            className="sa-brand-icon"
            onClick={() => navigate('/superadmin')}
          />
          {!sidebarCollapsed && (
            <div className="overflow-hidden">
              <div className="fw-bold text-dark text-nowrap">
                ApexBooking
              </div>
              <div className="text-uppercase fw-bold text-secondary sa-brand-role text-nowrap">
                Super Admin
              </div>
            </div>
          )}
        </div>

        {/* Nav */}
        <nav className="flex-grow-1 py-2">
          {!sidebarCollapsed && (
            <div className="px-3 mb-1 text-uppercase fw-semibold sa-nav-section">
              Platform
            </div>
          )}
          {NAV_ITEMS.map(({ path, label, icon, end }) => (
            <NavLink
              key={path}
              to={path}
              end={end}
              className={({ isActive }) =>
                `d-flex align-items-center gap-3 px-3 py-2 text-decoration-none rounded mx-2 my-1 small${
                  isActive
                    ? ' bg-primary text-white'
                    : ' text-secondary'
                }`
              }
            >
              <FontAwesomeIcon icon={icon} className="flex-shrink-0 sa-nav-icon" />
              {!sidebarCollapsed && <span>{label}</span>}
            </NavLink>
          ))}
        </nav>

        {/* User / Logout */}
        <div className="border-top p-3">
          {!sidebarCollapsed ? (
            <div className="d-flex align-items-center gap-2">
              <div className="rounded-circle bg-secondary d-flex align-items-center justify-content-center text-white fw-bold flex-shrink-0 sa-user-avatar">
                {user?.email?.charAt(0).toUpperCase() ?? 'S'}
              </div>
              <div className="flex-grow-1 overflow-hidden">
                <div className="small fw-semibold text-truncate sa-user-name">
                  {user?.email ?? 'Super Admin'}
                </div>
                <button
                  className="btn btn-link p-0 text-danger small"
                  onClick={logout}
                  disabled={logoutLoading}
                >
                  {logoutLoading ? 'Signing out...' : 'Sign out'}
                </button>
              </div>
            </div>
          ) : (
            <button
              className="btn btn-link p-0 text-danger d-flex justify-content-center w-100"
              onClick={logout}
              disabled={logoutLoading}
              title="Sign out"
            >
              <FontAwesomeIcon icon={faSignOutAlt} />
            </button>
          )}
        </div>
      </aside>

      {/* Main content */}
      <div className="d-flex flex-column flex-grow-1 sa-main-content">
        {/* Top bar */}
        <header className="bg-white border-bottom d-flex align-items-center justify-content-between px-4 sa-topbar">
          <button
            className="btn btn-link p-0 text-muted"
            onClick={() => setSidebarCollapsed(p => !p)}
            title="Toggle sidebar"
          >
            <FontAwesomeIcon icon={faBars} />
          </button>
          <div className="d-flex align-items-center gap-3">
            {/* Notification bell */}
            <div className="position-relative" ref={notifRef}>
              <button
                className="btn btn-link p-0 text-secondary position-relative d-flex align-items-center justify-content-center apex-header-btn"
                onClick={() => {
                  const opening = !notifOpen;
                  setNotifOpen(opening);
                  if (opening) fetchNotifications();
                }}
                aria-label="Notifications"
              >
                <FontAwesomeIcon icon={faBell} />
                {(notifData?.unreadCount ?? 0) > 0 && (
                  <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger apex-notif-badge">
                    {notifData!.unreadCount}
                  </span>
                )}
              </button>

              {notifOpen && (
                <div className="position-absolute end-0 bg-white border rounded shadow-sm apex-notif-dropdown">
                  <div className="px-3 py-2 border-bottom d-flex align-items-center justify-content-between">
                    <span className="fw-semibold">Notifications</span>
                    {(notifData?.unreadCount ?? 0) > 0 && (
                      <button
                        className="btn btn-link p-0 small text-primary"
                        onClick={markAllRead}
                        disabled={isMarkingRead}
                      >
                        Mark all read
                      </button>
                    )}
                  </div>
                  {notifLoading ? (
                    <div className="p-4 text-center text-muted small">Loading...</div>
                  ) : notifData && notifData.items.length > 0 ? (
                    <div className="apex-notif-scroll">
                      {notifData.items.map(n => (
                        <NotificationItem key={n.notificationId} notification={n} />
                      ))}
                    </div>
                  ) : (
                    <div className="p-4 text-center text-muted small">
                      <FontAwesomeIcon icon={faBellSlash} className="mb-2 d-block apex-notif-empty-icon" />
                      No notifications yet
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="text-muted small">Platform Administration</div>
          </div>
        </header>

        <main className="flex-grow-1 p-4">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
