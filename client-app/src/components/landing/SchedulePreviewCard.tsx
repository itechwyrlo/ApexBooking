import { Badge } from '../common/Badge'

type BookingStatus = 'confirmed' | 'pending'

const MOCK_SCHEDULE: { time: string; customer: string; service: string; status: BookingStatus }[] = [
  { time: '9:00 AM', customer: 'Ava Bennett', service: 'Haircut & Style', status: 'confirmed' },
  { time: '10:30 AM', customer: 'Marcus Lee', service: 'Consultation', status: 'confirmed' },
  { time: '12:00 PM', customer: 'Priya Shah', service: 'Color Touch-up', status: 'pending' },
  { time: '2:15 PM', customer: 'Diego Ruiz', service: 'Follow-up Visit', status: 'confirmed' },
]

const STATUS_LABEL: Record<BookingStatus, string> = {
  confirmed: 'Confirmed',
  pending: 'Pending',
}

interface ISchedulePreviewCardProps {
  className?: string
}

export function SchedulePreviewCard({ className = '' }: ISchedulePreviewCardProps) {
  const today = new Date().toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' })

  return (
    <div className={`card border-0 shadow rounded-4 ${className}`.trim()}>
      <div className="card-body p-4">
        <div className="d-flex align-items-center justify-content-between mb-3">
          <div>
            <p className="text-eyebrow mb-1">Today&apos;s Schedule</p>
            <p className="fw-semibold mb-0">{today}</p>
          </div>
          <Badge tone="primary">4 booked</Badge>
        </div>
        <div className="d-flex flex-column gap-2">
          {MOCK_SCHEDULE.map((item) => (
            <div
              key={item.time}
              className="d-flex align-items-center justify-content-between gap-2 p-2 rounded-3"
              style={{ backgroundColor: 'var(--color-canvas)' }}
            >
              <div className="d-flex align-items-center gap-3">
                <span className="fw-semibold small text-muted" style={{ width: 68 }}>
                  {item.time}
                </span>
                <div>
                  <p className="mb-0 fw-medium small">{item.customer}</p>
                  <p className="mb-0 text-muted" style={{ fontSize: '0.75rem' }}>
                    {item.service}
                  </p>
                </div>
              </div>
              <Badge tone={item.status === 'confirmed' ? 'success' : 'warning'}>{STATUS_LABEL[item.status]}</Badge>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
