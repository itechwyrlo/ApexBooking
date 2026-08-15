import { formatDisplayTime } from '../../utils/formatDateTime'
import { buildMonthGridDays, toIsoDate } from '../../utils/calendarGrid'
import { getBookingDisplayStatus, isBookingPaid, BOOKING_DISPLAY_STATUS_CHIP_CLASS } from '../../utils/bookingDisplayStatus'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

const WEEKDAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
const MAX_VISIBLE_EVENTS_PER_DAY = 3

interface IMonthCalendarGridProps {
  year: number
  month: number
  bookings: ITenantBooking[]
  onSelectBooking: (booking: ITenantBooking) => void
  onSelectDay: (isoDate: string) => void
}

export function MonthCalendarGrid({ year, month, bookings, onSelectBooking, onSelectDay }: IMonthCalendarGridProps) {
  const gridDays = buildMonthGridDays(year, month)
  const todayIso = toIsoDate(new Date())

  const bookingsByDate = new Map<string, ITenantBooking[]>()
  for (const booking of bookings) {
    const existing = bookingsByDate.get(booking.scheduledDate)
    if (existing) {
      existing.push(booking)
    } else {
      bookingsByDate.set(booking.scheduledDate, [booking])
    }
  }

  return (
    <div className="calendar-grid">
      <div className="row g-0 text-center text-muted small text-uppercase fw-semibold border-bottom pb-2 mb-2">
        {WEEKDAY_LABELS.map((label) => (
          <div key={label} className="col">
            {label}
          </div>
        ))}
      </div>

      <div className="row g-1">
        {gridDays.map((date) => {
          const isoDate = toIsoDate(date)
          const isCurrentMonth = date.getMonth() === month
          const isToday = isoDate === todayIso
          const dayBookings = [...(bookingsByDate.get(isoDate) ?? [])].sort((a, b) =>
            a.scheduledStartTime.localeCompare(b.scheduledStartTime),
          )
          const visibleBookings = dayBookings.slice(0, MAX_VISIBLE_EVENTS_PER_DAY)
          const overflowCount = dayBookings.length - visibleBookings.length
          const isClickable = dayBookings.length > 0

          return (
            <div key={isoDate} className="col-12 col-sm" style={{ flexBasis: '14.28%', maxWidth: '14.28%' }}>
              <button
                type="button"
                className={`w-100 h-100 text-start border rounded p-2 bg-transparent calendar-day-cell ${isClickable ? 'calendar-day-cell--clickable' : ''} ${isCurrentMonth ? '' : 'text-muted'}`.trim()}
                style={{ minHeight: '6.5rem', cursor: isClickable ? 'pointer' : 'default' }}
                onClick={() => isClickable && onSelectDay(isoDate)}
              >
                <div className={`small mb-1 ${isToday ? 'fw-bold text-primary' : ''}`}>
                  {isToday ? (
                    <span className="d-inline-flex align-items-center justify-content-center rounded-circle bg-primary text-white" style={{ width: '1.5rem', height: '1.5rem' }}>
                      {date.getDate()}
                    </span>
                  ) : (
                    date.getDate()
                  )}
                </div>

                <div className="d-flex flex-column gap-1">
                  {visibleBookings.map((booking) => {
                    const displayStatus = getBookingDisplayStatus(booking)
                    const paid = isBookingPaid(booking)

                    return (
                      <span
                        key={booking.bookingId}
                        role="button"
                        tabIndex={0}
                        className={`calendar-chip badge rounded-pill fw-normal text-truncate d-flex align-items-center text-start ${BOOKING_DISPLAY_STATUS_CHIP_CLASS[displayStatus]}`}
                        style={{ maxWidth: '100%' }}
                        title={paid ? 'Payment received' : 'Payment pending'}
                        onClick={(e) => {
                          e.stopPropagation()
                          onSelectBooking(booking)
                        }}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.stopPropagation()
                            onSelectBooking(booking)
                          }
                        }}
                      >
                        <span className={`calendar-chip-payment-dot ${paid ? '' : 'calendar-chip-payment-dot--pending'}`.trim()} aria-hidden="true" />
                        <span className="text-truncate">
                          {formatDisplayTime(booking.scheduledStartTime)} {booking.customerName}
                        </span>
                      </span>
                    )
                  })}
                  {overflowCount > 0 && (
                    <span
                      role="button"
                      tabIndex={0}
                      className="calendar-overflow-pill"
                      aria-label={`Show ${overflowCount} more appointment${overflowCount === 1 ? '' : 's'} on this day`}
                      onClick={(e) => {
                        e.stopPropagation()
                        onSelectDay(isoDate)
                      }}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.stopPropagation()
                          onSelectDay(isoDate)
                        }
                      }}
                    >
                      +{overflowCount} more
                    </span>
                  )}
                </div>
              </button>
            </div>
          )
        })}
      </div>
    </div>
  )
}
