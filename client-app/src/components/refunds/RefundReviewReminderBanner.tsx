import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Icon } from '../common/Icon'
import { Button } from '../common/Button'
import { buildDashboardPath } from '../../config/dashboardRoutes'

interface IRefundReviewReminderBannerProps {
  count: number
}

export function RefundReviewReminderBanner({ count }: IRefundReviewReminderBannerProps) {
  const [isDismissed, setIsDismissed] = useState(false)
  const navigate = useNavigate()
  const { slug } = useParams<{ slug: string }>()

  if (count === 0 || isDismissed) return null

  return (
    <div className="alert alert-warning d-flex align-items-center justify-content-between gap-3 mb-3" role="alert">
      <div className="d-flex align-items-center gap-2">
        <Icon name="alert-triangle" size={18} />
        <span>
          {count} refund request{count === 1 ? '' : 's'} {count === 1 ? 'needs' : 'need'} your attention before its deadline.
        </span>
      </div>
      <div className="d-flex align-items-center gap-2">
        <Button variant="primary" size="sm" onClick={() => navigate(buildDashboardPath(slug ?? '', 'refunds'))}>
          Review Now
        </Button>
        <button type="button" className="btn-close" aria-label="Dismiss" onClick={() => setIsDismissed(true)} />
      </div>
    </div>
  )
}
