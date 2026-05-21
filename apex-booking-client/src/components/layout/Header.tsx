import React, { useState, useRef, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBars,
  faBell,
  faBellSlash,
  faChevronDown,
  faUser,
  faCog,
  faSignOutAlt,
} from '@fortawesome/free-solid-svg-icons';
import apexbookingLogo from '../../assets/apexbooking-logo.svg';
import { useAuth } from '../../context/AuthContext';
import { useLogout } from '../../features/auth/hooks/useLogout';
import { useNotifications } from '../../features/notifications/hooks/useNotifications';
import { NotificationItem } from '../../features/notifications/components/NotificationItem';

interface HeaderProps {
  onToggleSidebar: () => void;
}

export const Header: React.FC<HeaderProps> = ({ onToggleSidebar }) => {
  const { user } = useAuth();
  const { logout, isLoading: loggingOut } = useLogout();
  const navigate = useNavigate();
  const { tenant } = useParams<{ tenant: string }>();

  const {
    data: notifData,
    isLoading: notifLoading,
    isMarkingRead,
    fetch: fetchNotifications,
    markAllRead,
  } = useNotifications();

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

  const initials = user?.fullName
    ? user.fullName.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase()
    : user?.email?.[0]?.toUpperCase() ?? '?';

  const displayName = user?.fullName || user?.email || 'User';
  const displayRole = user?.role === 'tenantadmin' ? 'Admin' : user?.role ?? '';

  const goTo = (path: string) => {
    setUserMenuOpen(false);
    navigate(`/t/${tenant}${path}`);
  };

  const unreadCount = notifData?.unreadCount ?? 0;

  return (
    <header className="apex-header">
      <div className="apex-header-brand">
        <button
          className="apex-toggle-btn btn btn-outline-secondary"
          onClick={onToggleSidebar}
          aria-label="Toggle sidebar"
        >
          <FontAwesomeIcon icon={faBars} />
        </button>
        <img src={apexbookingLogo} alt="ApexBooking" style={{ width: 22, height: 22, borderRadius: 6 }} />
        <span className="apex-brand-text">ApexBooking</span>
      </div>

      <div className="apex-header-right">

        {/* Notification bell */}
        <div className="position-relative" ref={notifRef}>
          <button
            className="apex-icon-btn btn btn-outline-secondary"
            onClick={() => {
              const opening = !notifOpen;
              setNotifOpen(opening);
              setUserMenuOpen(false);
              if (opening) fetchNotifications();
            }}
            aria-label="Notifications"
          >
            <FontAwesomeIcon icon={faBell} />
            {unreadCount > 0 && (
              <span
                className="position-absolute top-0 start-100 translate-middle p-1 bg-danger border border-white rounded-circle"
                style={{ width: 10, height: 10 }}
              />
            )}
          </button>

          {notifOpen && (
            <div className="apex-notif-dropdown position-absolute end-0 bg-white border rounded shadow-sm">
              <div className="px-3 py-2 border-bottom d-flex align-items-center justify-content-between">
                <span className="fw-semibold" style={{ fontSize: 13 }}>Notifications</span>
                {unreadCount > 0 && (
                  <button
                    className="btn btn-link p-0 text-primary"
                    style={{ fontSize: 11, fontWeight: 500 }}
                    onClick={markAllRead}
                    disabled={isMarkingRead}
                  >
                    Mark all as read
                  </button>
                )}
              </div>

              {notifLoading ? (
                <div className="p-4 text-center text-muted small">Loading...</div>
              ) : notifData && notifData.items.length > 0 ? (
                <div style={{ maxHeight: 340, overflowY: 'auto' }}>
                  {notifData.items.map(n => (
                    <NotificationItem key={n.notificationId} notification={n} />
                  ))}
                </div>
              ) : (
                <div className="p-4 text-center text-muted small">
                  <FontAwesomeIcon
                    icon={faBellSlash}
                    className="d-block mb-2 mx-auto"
                    style={{ fontSize: 24, opacity: 0.3 }}
                  />
                  No notifications yet
                </div>
              )}

              <div className="text-center px-3 py-2 border-top">
                <span className="text-primary" style={{ fontSize: 12, fontWeight: 500, cursor: 'pointer' }}>
                  View all notifications
                </span>
              </div>
            </div>
          )}
        </div>

        {/* User menu */}
        <div className="position-relative" ref={userMenuRef}>
          <button
            className="apex-user-btn btn btn-outline-secondary rounded-2"
            onClick={() => { setUserMenuOpen(p => !p); setNotifOpen(false); }}
          >
            <div
              className="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center fw-bold flex-shrink-0"
              style={{ width: 26, height: 26, fontSize: 11 }}
            >
              {initials}
            </div>
            <div className="apex-user-info-text text-start d-none d-md-block">
              <div className="apex-user-name">{displayName}</div>
              <div className="apex-user-role">{displayRole}</div>
            </div>
            <FontAwesomeIcon
              icon={faChevronDown}
              className="text-secondary ms-1 d-none d-md-block"
              style={{ fontSize: 10 }}
            />
          </button>

          {userMenuOpen && (
            <div className="apex-user-dropdown position-absolute end-0 bg-white border rounded shadow-sm">
              <div className="px-3 py-2 border-bottom">
                <div className="fw-semibold" style={{ fontSize: 13 }}>{displayName}</div>
                <div className="text-muted" style={{ fontSize: 11 }}>{user?.email}</div>
              </div>
              <button className="apex-user-dropdown-item" onClick={() => goTo('/profile')}>
                <FontAwesomeIcon icon={faUser} /> Profile
              </button>
              <button className="apex-user-dropdown-item" onClick={() => goTo('/settings')}>
                <FontAwesomeIcon icon={faCog} /> Settings
              </button>
              <div className="border-top my-1" />
              <button
                className="apex-user-dropdown-item danger"
                onClick={() => { setUserMenuOpen(false); logout(); }}
                disabled={loggingOut}
              >
                <FontAwesomeIcon icon={faSignOutAlt} />
                {loggingOut ? 'Logging out...' : 'Sign out'}
              </button>
            </div>
          )}
        </div>

      </div>
    </header>
  );
};
