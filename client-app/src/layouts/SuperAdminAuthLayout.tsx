import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { Icon } from '../components/common/Icon'

interface ISuperAdminAuthLayoutProps {
  children: ReactNode
}

export function SuperAdminAuthLayout({ children }: ISuperAdminAuthLayoutProps) {
  return (
    <div className="admin-auth-shell min-vh-100 d-flex align-items-center justify-content-center py-5">
      <div className="admin-auth-card">
        <div className="admin-auth-card__header">
          <div className="admin-auth-card__badge">
            <Icon name="lock" size={14} />
            <span>Restricted Access</span>
          </div>
          <Link to="/superadmin/login" className="admin-auth-card__brand text-decoration-none">
            <img src="/favicon.svg" alt="ApexBooking logo" width={26} height={26} />
            <span>ApexBooking Admin</span>
          </Link>
        </div>
        <div className="admin-auth-card__body bg-white">{children}</div>
      </div>
    </div>
  )
}
