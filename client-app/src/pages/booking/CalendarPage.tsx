import { useEffect, useState } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { SidePanel } from '../../components/common/SidePanel'
import { MonthCalendarGrid } from '../../components/calendar/MonthCalendarGrid'
import { BookingDayList } from '../../components/calendar/BookingDayList'
import { BookingDetailPanel } from '../../components/calendar/BookingDetailPanel'
import { CalendarLegend } from '../../components/calendar/CalendarLegend'
import { CancelBookingModal } from '../../components/appointments/CancelBookingModal'
import { useTenantBookings } from '../../hooks/useTenantBookings'
import { useBranches } from '../../hooks/useBranches'
import { useToast } from '../../hooks/useToast'
import { admitBooking, completeBooking, markBookingNoShow } from '../../services/bookingService'
import { BookingStatus } from '../../types/BookingStatus'
import { getMonthGridRange } from '../../utils/calendarGrid'
import { formatDisplayDate } from '../../utils/formatDateTime'
import type { ITenantBooking, ITenantBookingFilters } from '../../interfaces/ITenantBooking'

// Bounded by a single month's grid range, not the full historical dataset, so one large page
// comfortably covers realistic booking volume without needing cursor/infinite pagination here.
const CALENDAR_PAGE_SIZE = 1000

const MONTH_LABEL_FORMAT = new Intl.DateTimeFormat(undefined, { month: 'long', year: 'numeric' })

type IPanelState =
  | { mode: 'day'; date: string; bookings: ITenantBooking[] }
  | { mode: 'booking'; booking: ITenantBooking; backToDay: { date: string; bookings: ITenantBooking[] } | null }

function sortByStartTime(bookings: ITenantBooking[]): ITenantBooking[] {
  return [...bookings].sort((a, b) => a.scheduledStartTime.localeCompare(b.scheduledStartTime))
}

export function CalendarPage() {
  const today = new Date()
  const [year, setYear] = useState(today.getFullYear())
  const [month, setMonth] = useState(today.getMonth())
  const [branchFilter, setBranchFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState<ITenantBookingFilters['status']>(undefined)
  const [panel, setPanel] = useState<IPanelState | null>(null)
  const [isLegendOpen, setIsLegendOpen] = useState(false)
  const [cancelTarget, setCancelTarget] = useState<ITenantBooking | null>(null)
  const [pendingBookingId, setPendingBookingId] = useState<string | null>(null)

  const { branches } = useBranches()
  const { fromDate, toDate } = getMonthGridRange(year, month)
  const { bookings, isLoading, error, refetch } = useTenantBookings(
    { branchId: branchFilter || undefined, status: statusFilter, fromDate, toDate },
    { pageNumber: 1, pageSize: CALENDAR_PAGE_SIZE },
  )
  const { showToast } = useToast()

  // Keeps the open panel in sync after a refetch (e.g. right after admitting/completing/
  // cancelling a booking from within it), rather than leaving it showing a stale status.
  useEffect(() => {
    setPanel((prev) => {
      if (!prev) return prev
      if (prev.mode === 'booking') {
        const updated = bookings.find((b) => b.bookingId === prev.booking.bookingId)
        return updated ? { ...prev, booking: updated } : prev
      }
      const updated = sortByStartTime(bookings.filter((b) => b.scheduledDate === prev.date))
      return { ...prev, bookings: updated }
    })
  }, [bookings])

  const goToPreviousMonth = () => {
    setMonth((current) => {
      if (current === 0) {
        setYear((y) => y - 1)
        return 11
      }
      return current - 1
    })
  }

  const goToNextMonth = () => {
    setMonth((current) => {
      if (current === 11) {
        setYear((y) => y + 1)
        return 0
      }
      return current + 1
    })
  }

  const goToToday = () => {
    setYear(today.getFullYear())
    setMonth(today.getMonth())
  }

  const handleSelectBookingFromGrid = (booking: ITenantBooking) => {
    setPanel({ mode: 'booking', booking, backToDay: null })
  }

  const handleSelectDay = (isoDate: string) => {
    const dayBookings = sortByStartTime(bookings.filter((b) => b.scheduledDate === isoDate))
    setPanel({ mode: 'day', date: isoDate, bookings: dayBookings })
  }

  const runAction = async (booking: ITenantBooking, action: () => Promise<void>, successMessage: string) => {
    setPendingBookingId(booking.bookingId)
    try {
      await action()
      showToast('success', successMessage)
      refetch()
    } catch (actionError) {
      const detail = axios.isAxiosError(actionError) ? (actionError.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'That action could not be completed. Please try again.')
    } finally {
      setPendingBookingId(null)
    }
  }

  const handleAdmit = (booking: ITenantBooking) =>
    runAction(booking, async () => void (await admitBooking(booking.bookingId)), `${booking.bookingReference} admitted.`)

  const handleComplete = (booking: ITenantBooking) =>
    runAction(booking, () => completeBooking(booking.bookingId), `${booking.bookingReference} marked as completed.`)

  const handleNoShow = (booking: ITenantBooking) =>
    runAction(booking, () => markBookingNoShow(booking.bookingId), `${booking.bookingReference} marked as no-show.`)

  return (
    <div>
      <PageHeader title="Calendar" description="See who's booked, when, and review payment status at a glance." />

      <Card className="mb-3">
        <div className="row g-3 align-items-end">
          <div className="col-12 col-md-4 d-flex align-items-center gap-2">
            <Button variant="outline-secondary" size="sm" onClick={goToPreviousMonth} aria-label="Previous month">
              Previous
            </Button>
            <Button variant="outline-secondary" size="sm" onClick={goToToday}>
              Today
            </Button>
            <Button variant="outline-secondary" size="sm" onClick={goToNextMonth} aria-label="Next month">
              Next
            </Button>
            <span className="fw-semibold ms-1">{MONTH_LABEL_FORMAT.format(new Date(year, month, 1))}</span>
          </div>
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted" htmlFor="calendar-branch-filter">
              Branch
            </label>
            <select
              id="calendar-branch-filter"
              className="form-select"
              value={branchFilter}
              onChange={(e) => setBranchFilter(e.target.value)}
            >
              <option value="">All branches</option>
              {branches.map((branch) => (
                <option key={branch.id} value={branch.id}>
                  {branch.name}
                </option>
              ))}
            </select>
          </div>
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted" htmlFor="calendar-status-filter">
              Status
            </label>
            <select
              id="calendar-status-filter"
              className="form-select"
              value={statusFilter ?? ''}
              onChange={(e) => setStatusFilter((e.target.value || undefined) as ITenantBookingFilters['status'])}
            >
              <option value="">All statuses</option>
              {Object.values(BookingStatus).map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>
          <div className="col-12 col-md-2">
            <Button
              variant="outline-secondary"
              size="sm"
              aria-expanded={isLegendOpen}
              onClick={() => setIsLegendOpen((open) => !open)}
            >
              {isLegendOpen ? 'Hide Legend' : 'Legend'}
            </Button>
          </div>
        </div>
        {isLegendOpen && <CalendarLegend />}
      </Card>

      <Card>
        {error && <p className="text-danger small mb-3">{error}</p>}
        {isLoading ? (
          <p className="text-muted small mb-0">Loading calendar…</p>
        ) : (
          <MonthCalendarGrid
            year={year}
            month={month}
            bookings={bookings}
            onSelectBooking={handleSelectBookingFromGrid}
            onSelectDay={handleSelectDay}
          />
        )}
      </Card>

      <SidePanel
        isOpen={panel !== null}
        title={panel?.mode === 'booking' ? `${panel.booking.serviceName} with ${panel.booking.staffName}` : panel ? formatDisplayDate(panel.date) : ''}
        description={panel?.mode === 'day' ? `${panel.bookings.length} appointment${panel.bookings.length === 1 ? '' : 's'}` : undefined}
        onClose={() => setPanel(null)}
      >
        {panel?.mode === 'day' && (
          <BookingDayList
            bookings={panel.bookings}
            onSelectBooking={(booking) => setPanel({ mode: 'booking', booking, backToDay: { date: panel.date, bookings: panel.bookings } })}
          />
        )}
        {panel?.mode === 'booking' &&
          (() => {
            const backToDay = panel.backToDay
            return (
              <BookingDetailPanel
                booking={panel.booking}
                onBack={backToDay ? () => setPanel({ mode: 'day', ...backToDay }) : undefined}
                pendingBookingId={pendingBookingId}
                onAdmit={handleAdmit}
                onComplete={handleComplete}
                onNoShow={handleNoShow}
                onCancel={setCancelTarget}
              />
            )
          })()}
      </SidePanel>

      <CancelBookingModal booking={cancelTarget} onClose={() => setCancelTarget(null)} onCancelled={refetch} />
    </div>
  )
}
