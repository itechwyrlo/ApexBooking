import { Link, useLocation } from 'react-router-dom'
import { Button } from '../components/common/Button'
import { Card } from '../components/common/Card'
import { Icon } from '../components/common/Icon'
import { StatusStepper } from '../components/requestAccess/StatusStepper'

const SUPPORT_EMAIL = 'wyrlo.adeva.lerios@gmail.com'

interface IRequestAccessPendingLocationState {
  businessName?: string
  ownerEmail?: string
}

export function RequestAccessPendingPage() {
  const location = useLocation()
  const state = location.state as IRequestAccessPendingLocationState | null
  const businessName = state?.businessName
  const ownerEmail = state?.ownerEmail

  return (
    <div className="request-access-shell">
      <header className="request-access-topbar">
        <div className="container">
          <Link to="/" className="d-inline-flex align-items-center gap-2 text-decoration-none">
            <img src="/favicon.svg" alt="ApexBooking logo" width={28} height={28} />
            <span className="fw-bold fs-5 font-display text-dark">ApexBooking</span>
          </Link>
        </div>
      </header>

      <div className="container py-4 py-md-5 d-flex justify-content-center">
        <Card className="pending-card text-center" bodyClassName="p-4 p-md-5">
          <div className="pending-card__intro">
            <div className="pending-card__badge">
              <Icon name="check-circle-light" size={26} />
            </div>
            <h1 className="h3 fw-bold mb-2">Request Received</h1>
            <p className="text-secondary mb-0">
              {businessName ? (
                <>
                  Thanks, <span className="fw-semibold text-dark">{businessName}</span> — your request is being
                  reviewed.
                </>
              ) : (
                'Thanks — your request is being reviewed.'
              )}
            </p>
          </div>

          <StatusStepper />

          <ul className="list-unstyled text-secondary text-start mx-auto pending-card__copy">
            <li>We are verifying your business details.</li>
            <li>
              You will get an email at{' '}
              {ownerEmail ? (
                <span className="fw-semibold text-dark">{ownerEmail}</span>
              ) : (
                'the address you provided'
              )}{' '}
              once approved.
            </li>
          </ul>

          <Button to="/" className="w-100 mb-3">
            Back to Home
          </Button>
          <a href={`mailto:${SUPPORT_EMAIL}`} className="pending-card__support-link">
            Wrong email? Contact support
          </a>
        </Card>
      </div>
    </div>
  )
}
