import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { Card } from '../components/common/Card'
import { Icon } from '../components/common/Icon'

interface IAuthLayoutProps {
  children: ReactNode
}

const HIGHLIGHTS = ["Today's bookings in one view", 'Team schedules that stay in sync', 'A booking page your customers can use anytime']

export function AuthLayout({ children }: IAuthLayoutProps) {
  return (
    <div className="min-vh-100 d-flex align-items-center py-4 py-lg-5" style={{ backgroundColor: 'var(--color-canvas)' }}>
      <div className="container">
        <div className="row justify-content-center align-items-center g-4 g-lg-5">
          <div className="col-lg-6 d-none d-lg-block">
            <div
              className="bg-gradient-brand text-white rounded-4 p-5 d-flex flex-column justify-content-center"
              style={{ minHeight: 520 }}
            >
              <Link to="/" className="d-inline-flex align-items-center gap-2 mb-5 text-decoration-none text-white">
                <img src="/favicon.svg" alt="ApexBooking logo" width={32} height={32} />
                <span className="fw-bold fs-5">ApexBooking</span>
              </Link>
              <h2 className="fw-bold display-6 mb-4">Manage your bookings from one place.</h2>
              <ul className="list-unstyled d-flex flex-column gap-4 mb-0">
                {HIGHLIGHTS.map((item) => (
                  <li key={item} className="d-flex align-items-center gap-3 opacity-90 fs-5">
                    <Icon name="check-circle-light" size={22} className="flex-shrink-0" />
                    {item}
                  </li>
                ))}
              </ul>
            </div>
          </div>
          <div className="col-12 col-md-8 col-lg-5">
            <div className="bg-gradient-brand text-white rounded-4 p-4 mb-4 d-lg-none">
              <Link to="/" className="d-inline-flex align-items-center gap-2 mb-3 text-decoration-none text-white">
                <img src="/favicon.svg" alt="ApexBooking logo" width={28} height={28} />
                <span className="fw-bold fs-6">ApexBooking</span>
              </Link>
              <p className="fw-semibold mb-3">Manage your bookings from one place.</p>
              <ul className="list-unstyled d-flex flex-column gap-2 mb-0">
                {HIGHLIGHTS.map((item) => (
                  <li key={item} className="d-flex align-items-center gap-2 opacity-90 small">
                    <Icon name="check-circle-light" size={16} className="flex-shrink-0" />
                    {item}
                  </li>
                ))}
              </ul>
            </div>
            <Card bodyClassName="p-4 p-md-5">{children}</Card>
          </div>
        </div>
      </div>
    </div>
  )
}
