interface IFeatureIllustrationProps {
  variant: 'online-booking' | 'booking-calendar' | 'dashboard-reports'
}

const CALENDAR_DAY_LABELS = ['S', 'M', 'T', 'W', 'T', 'F', 'S']
const CALENDAR_BOOKED_INDEXES = new Set([3, 9, 14, 17])
const CHART_BAR_HEIGHTS = [40, 65, 50, 80, 60, 90, 45]

export function FeatureIllustration({ variant }: IFeatureIllustrationProps) {
  if (variant === 'online-booking') {
    return (
      <div className="feature-illustration">
        <div className="feature-illustration__row">
          <span className="feature-illustration__chip">Haircut &amp; Style</span>
          <span className="feature-illustration__chip feature-illustration__chip--muted">45 min</span>
        </div>
        <div className="feature-illustration__slots">
          {['9:00', '10:30', '1:00', '2:30'].map((slot, index) => (
            <span
              key={slot}
              className={`feature-illustration__slot ${index === 1 ? 'feature-illustration__slot--active' : ''}`}
            >
              {slot}
            </span>
          ))}
        </div>
      </div>
    )
  }

  if (variant === 'booking-calendar') {
    return (
      <div className="feature-illustration feature-illustration--calendar">
        {CALENDAR_DAY_LABELS.map((label, index) => (
          <span key={`label-${index}`} className="feature-illustration__day-label">
            {label}
          </span>
        ))}
        {Array.from({ length: 21 }).map((_, index) => (
          <span
            key={`day-${index}`}
            className={`feature-illustration__day ${CALENDAR_BOOKED_INDEXES.has(index) ? 'feature-illustration__day--booked' : ''}`}
          />
        ))}
      </div>
    )
  }

  return (
    <div className="feature-illustration feature-illustration--chart">
      {CHART_BAR_HEIGHTS.map((height, index) => (
        <span key={index} className="feature-illustration__bar" style={{ height: `${height}%` }} />
      ))}
    </div>
  )
}
