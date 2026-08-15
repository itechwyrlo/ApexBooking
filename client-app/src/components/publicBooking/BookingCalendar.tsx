import { useRef, type KeyboardEvent } from 'react'

interface IBookingCalendarProps {
  value: string // "yyyy-MM-dd"
  onChange: (date: string) => void
  minDate: string // "yyyy-MM-dd"
}

const WEEKDAY_LABELS = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa']
const MONTH_LABELS = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
]

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

function toIsoDate(year: number, month: number, day: number): string {
  return `${year}-${pad(month + 1)}-${pad(day)}`
}

function parseIsoDate(iso: string): { year: number; month: number; day: number } {
  const [year, month, day] = iso.split('-').map(Number)
  return { year, month: month - 1, day }
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

export function BookingCalendar({ value, onChange, minDate }: IBookingCalendarProps) {
  const selected = parseIsoDate(value)
  const min = parseIsoDate(minDate)
  const today = todayIsoDate()

  // The visible month always follows the selected date — every navigation either
  // moves the selection (via day click) or the month cursor (via prev/next), and
  // both are represented by `value`/local state below, so there's no separate
  // "viewed but nothing selected" state to track.
  const [viewYear, viewMonth] = [selected.year, selected.month]

  const dayRefs = useRef<Record<number, HTMLButtonElement | null>>({})

  const firstWeekday = new Date(viewYear, viewMonth, 1).getDay()
  const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate()
  const cells: (number | null)[] = [
    ...Array.from({ length: firstWeekday }, () => null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ]
  while (cells.length % 7 !== 0) cells.push(null)

  const currentMonthTuple = viewYear * 12 + viewMonth
  const minMonthTuple = min.year * 12 + min.month
  const canGoPrev = currentMonthTuple > minMonthTuple

  const goToMonth = (delta: number) => {
    const targetTuple = currentMonthTuple + delta
    const targetYear = Math.floor(targetTuple / 12)
    const targetMonth = targetTuple % 12
    const targetDaysInMonth = new Date(targetYear, targetMonth + 1, 0).getDate()
    const targetDay = Math.min(selected.day, targetDaysInMonth)
    const candidate = toIsoDate(targetYear, targetMonth, targetDay)
    onChange(candidate < minDate ? minDate : candidate)
  }

  const handleDayKeyDown = (event: KeyboardEvent<HTMLButtonElement>, day: number) => {
    const deltas: Record<string, number> = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 }
    const delta = deltas[event.key]
    if (delta === undefined) return
    event.preventDefault()
    const target = dayRefs.current[day + delta]
    if (target) target.focus()
  }

  return (
    <div>
      <div className="d-flex align-items-center justify-content-between mb-3">
        <button
          type="button"
          className="pb-calendar-nav"
          onClick={() => goToMonth(-1)}
          disabled={!canGoPrev}
          aria-label="Previous month"
        >
          ‹
        </button>
        <span className="fw-semibold small">
          {MONTH_LABELS[viewMonth]} {viewYear}
        </span>
        <button type="button" className="pb-calendar-nav" onClick={() => goToMonth(1)} aria-label="Next month">
          ›
        </button>
      </div>

      <div className="row g-1 mb-1">
        {WEEKDAY_LABELS.map((label) => (
          <div key={label} className="col">
            <div className="pb-calendar-weekday">{label}</div>
          </div>
        ))}
      </div>

      <div className="row g-1">
        {cells.map((day, index) => (
          <div key={index} className="col" style={{ flex: '0 0 14.2857%', maxWidth: '14.2857%' }}>
            {day === null ? (
              <div aria-hidden="true" />
            ) : (
              (() => {
                const iso = toIsoDate(viewYear, viewMonth, day)
                const isPast = iso < minDate
                const isToday = iso === today
                const isSelected = iso === value

                return (
                  <button
                    type="button"
                    ref={(el) => {
                      dayRefs.current[day] = el
                    }}
                    className={`pb-calendar-day ${isToday ? 'is-today' : ''} ${isSelected ? 'is-selected' : ''}`}
                    disabled={isPast}
                    onClick={() => onChange(iso)}
                    onKeyDown={(event) => handleDayKeyDown(event, day)}
                    aria-current={isToday ? 'date' : undefined}
                    aria-pressed={isSelected}
                  >
                    {day}
                  </button>
                )
              })()
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
