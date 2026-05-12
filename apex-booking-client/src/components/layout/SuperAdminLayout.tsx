import React, { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useLogout } from '../../features/auth/hooks/useLogout';
import { useAuth } from '../../context/AuthContext';

const NAV_ITEMS = [
  { path: '/superadmin', label: 'Overview', icon: 'fa-tachometer-alt', end: true },
  { path: '/superadmin/organizations', label: 'Organizations', icon: 'fa-building', end: false },
  { path: '/superadmin/payment-gateway', label: 'Payment Gateway', icon: 'fa-credit-card', end: false },
];

export const SuperAdminLayout: React.FC = () => {
  const { logout, isLoading: logoutLoading } = useLogout();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  return (
    <div className="d-flex min-vh-100" style={{ backgroundColor: '#f8f9fa' }}>
      {/* Sidebar */}
      <aside
        className="bg-white border-end d-flex flex-column"
        style={{
          width: sidebarCollapsed ? 64 : 240,
          minHeight: '100vh',
          transition: 'width 0.2s ease',
          flexShrink: 0,
        }}
      >
        {/* Logo / Brand */}
        <div
          className="border-bottom d-flex align-items-center px-3"
          style={{ height: 56, gap: 10, overflow: 'hidden' }}
        >
          <div
            className="bg-primary rounded d-flex align-items-center justify-content-center flex-shrink-0"
            style={{ width: 32, height: 32, cursor: 'pointer' }}
            onClick={() => navigate('/superadmin')}
          >
            <span className="text-white fw-bold" style={{ fontSize: 14 }}>A</span>
          </div>
          {!sidebarCollapsed && (
            <div style={{ overflow: 'hidden' }}>
              <div className="fw-bold text-dark" style={{ fontSize: 14, whiteSpace: 'nowrap' }}>
                ApexBooking
              </div>
              <div
                className="text-uppercase fw-bold"
                style={{ fontSize: 10, color: '#6c757d', letterSpacing: '0.08em', whiteSpace: 'nowrap' }}
              >
                Super Admin
              </div>
            </div>
          )}
        </div>

        {/* Nav */}
        <nav className="flex-grow-1 py-2">
          {!sidebarCollapsed && (
            <div
              className="px-3 mb-1 text-uppercase fw-semibold"
              style={{ fontSize: 10, color: '#adb5bd', letterSpacing: '0.1em' }}
            >
              Platform
            </div>
          )}
          {NAV_ITEMS.map(({ path, label, icon, end }) => (
            <NavLink
              key={path}
              to={path}
              end={end}
              className={({ isActive }) =>
                `d-flex align-items-center gap-3 px-3 py-2 text-decoration-none rounded mx-2 my-1 ${
                  isActive
                    ? 'bg-primary text-white'
                    : 'text-secondary'
                }`
              }
              style={{ fontSize: 14 }}
            >
              <i className={`fas ${icon} flex-shrink-0`} style={{ width: 16, textAlign: 'center' }} />
              {!sidebarCollapsed && <span>{label}</span>}
            </NavLink>
          ))}
        </nav>

        {/* User / Logout */}
        <div className="border-top p-3">
          {!sidebarCollapsed ? (
            <div className="d-flex align-items-center gap-2">
              <div
                className="rounded-circle bg-secondary d-flex align-items-center justify-content-center text-white fw-bold flex-shrink-0"
                style={{ width: 32, height: 32, fontSize: 13 }}
              >
                {user?.email?.charAt(0).toUpperCase() ?? 'S'}
              </div>
              <div className="flex-grow-1 overflow-hidden">
                <div className="small fw-semibold text-truncate" style={{ maxWidth: 130 }}>
                  {user?.email ?? 'Super Admin'}
                </div>
                <button
                  className="btn btn-link p-0 text-danger"
                  style={{ fontSize: 12 }}
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
              <i className="fas fa-sign-out-alt" />
            </button>
          )}
        </div>
      </aside>

      {/* Main content */}
      <div className="d-flex flex-column flex-grow-1" style={{ minWidth: 0 }}>
        {/* Top bar */}
        <header
          className="bg-white border-bottom d-flex align-items-center justify-content-between px-4"
          style={{ height: 56, flexShrink: 0 }}
        >
          <button
            className="btn btn-link p-0 text-muted"
            onClick={() => setSidebarCollapsed(p => !p)}
            title="Toggle sidebar"
          >
            <i className="fas fa-bars" />
          </button>
          <div className="text-muted small">
            Platform Administration
          </div>
        </header>

        <main className="flex-grow-1 p-4">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
