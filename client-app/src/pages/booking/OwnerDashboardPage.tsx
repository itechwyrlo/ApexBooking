import { useState } from 'react'
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'
import { StatTile } from '../../components/common/StatTile'
import { StaffLineupTimeline } from '../../components/dashboard/StaffLineupTimeline'
import { CancelRefundPickerModal } from '../../components/dashboard/CancelRefundPickerModal'
import { ReassignBookingModal } from '../../components/dashboard/ReassignBookingModal'
import { AdmitScanModal } from '../../components/appointments/AdmitScanModal'
import { CancelBookingModal } from '../../components/appointments/CancelBookingModal'
import { NewWalkInModal } from '../../components/appointments/NewWalkInModal'
import { useAuth } from '../../hooks/useAuth'
import { useTenantBookings } from '../../hooks/useTenantBookings'
import { useRefundLog } from '../../hooks/useRefundLog'
import { useTenantRevenue } from '../../hooks/useTenantRevenue'
import { useStaffPerformance } from '../../hooks/useStaffPerformance'
import { formatDisplayDate } from '../../utils/formatDateTime'
import { BookingStatus } from '../../types/BookingStatus'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

const TODAY_LABEL = new Date().toLocaleDateString(undefined, {
  weekday: 'long',
  month: 'long',
  day: 'numeric',
})

function getTodayIsoDate(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

// Keeps the 3 revenue tiles always mounted at a consistent height (no loading-vs-loaded layout
// shift) — "…" while the request is in flight, "—" if it came back empty/failed.
function formatRevenueValue(amount: number | undefined, currencyCode: string | undefined, isLoading: boolean): string {
  if (isLoading) return '…'
  if (amount === undefined || !currencyCode) return '—'
  return `${amount.toFixed(2)} ${currencyCode}`
}

export function OwnerDashboardPage() {
  const { user } = useAuth()
  const todayIso = getTodayIsoDate()

  const { bookings: myBookings, isLoading: isMyBookingsLoading } = useTenantBookings({
    staffId: user?.tenantMemberId ?? undefined,
    fromDate: todayIso,
    toDate: todayIso,
  })
  const { bookings: todaysBookings, refetch: refetchTodaysBookings } = useTenantBookings({
    status: BookingStatus.Scheduled,
    fromDate: todayIso,
    toDate: todayIso,
  })
  const { entries: refundLog, isLoading: isRefundLogLoading } = useRefundLog()
  const { revenue, isLoading: isRevenueLoading } = useTenantRevenue(todayIso)
  const { entries: staffPerformance, isLoading: isStaffPerformanceLoading } = useStaffPerformance(todayIso)
  const staffPerformanceByRevenue = [...staffPerformance].sort((a, b) => b.revenueGenerated - a.revenueGenerated)

  const [isScanModalOpen, setIsScanModalOpen] = useState(false)
  const [isCancelPickerOpen, setIsCancelPickerOpen] = useState(false)
  const [cancelTarget, setCancelTarget] = useState<ITenantBooking | null>(null)
  const [isWalkInModalOpen, setIsWalkInModalOpen] = useState(false)
  const [isReassignModalOpen, setIsReassignModalOpen] = useState(false)

  return (
    <div>
      <PageHeader title="Business Overview" description={TODAY_LABEL} />

      <div className="row g-3 mb-3">
        <div className="col-6 col-md-4">
          <StatTile
            label="Total Revenue"
            value={formatRevenueValue(revenue?.total, revenue?.currencyCode, isRevenueLoading)}
            icon="chart"
            tone="primary"
            emphasized
          />
        </div>
        <div className="col-6 col-md-4">
          <StatTile
            label="Online Revenue"
            value={formatRevenueValue(revenue?.onlineAmount, revenue?.currencyCode, isRevenueLoading)}
            icon="globe"
            tone="success"
          />
        </div>
        <div className="col-6 col-md-4">
          <StatTile
            label="Pay-on-Visit Revenue"
            value={formatRevenueValue(revenue?.payInVisitAmount, revenue?.currencyCode, isRevenueLoading)}
            icon="qr-code"
            tone="neutral"
          />
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Refund Log</h2>
            {isRefundLogLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : refundLog.length === 0 ? (
              <EmptyState
                icon="refund"
                title="No refunds yet"
                description="Processed refunds will list here with amount, date, and original payment method."
              />
            ) : (
              <ul className="list-unstyled mb-0">
                {refundLog.map((entry) => (
                  <li key={entry.id} className="d-flex justify-content-between align-items-center py-1">
                    <div>
                      <span className="fw-semibold small">{entry.bookingReference}</span>
                      <span className="text-muted small ms-2">{entry.status === 'Refunded' ? 'Refunded' : 'Rejected'}</span>
                    </div>
                    <div className="text-end">
                      <div className="small fw-semibold">
                        {entry.amount.toFixed(2)} {entry.currencyCode}
                      </div>
                      <div className="text-muted small">{formatDisplayDate(entry.processedAt)}</div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Staff Performance by Revenue</h2>
            {isStaffPerformanceLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : staffPerformanceByRevenue.length === 0 ? (
              <EmptyState
                icon="staff"
                title="No performance data yet"
                description="Your team, ranked by revenue generated, will appear here."
              />
            ) : (
              <ul className="list-unstyled mb-0">
                {staffPerformanceByRevenue.map((entry) => (
                  <li key={entry.tenantMemberId} className="d-flex justify-content-between align-items-center py-1">
                    <div>
                      <span className="fw-semibold small">{entry.name}</span>
                      <span className="text-muted small ms-2">{entry.servicesCompleted} completed</span>
                    </div>
                    <div className="small fw-semibold">
                      {entry.revenueGenerated.toFixed(2)} {entry.currencyCode}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">My Personal Lineup</h2>
            <StaffLineupTimeline bookings={myBookings} isLoading={isMyBookingsLoading} />
          </Card>
        </div>
      </div>

      <div className="row g-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Quick Tools</h2>
            <div className="d-flex flex-wrap gap-2">
              <Button variant="outline-secondary" size="sm" icon="qr-code" onClick={() => setIsScanModalOpen(true)}>
                Scan Booking QR
              </Button>
              <Button variant="outline-secondary" size="sm" icon="plus" onClick={() => setIsWalkInModalOpen(true)}>
                Quick Walk-In
              </Button>
              <Button variant="outline-secondary" size="sm" icon="refresh" onClick={() => setIsReassignModalOpen(true)}>
                Reassign Barber
              </Button>
              <Button variant="outline-secondary" size="sm" icon="x-circle" onClick={() => setIsCancelPickerOpen(true)}>
                Cancel &amp; Refund
              </Button>
            </div>
          </Card>
        </div>
      </div>

      <AdmitScanModal isOpen={isScanModalOpen} onClose={() => setIsScanModalOpen(false)} onAdmitted={refetchTodaysBookings} />

      <CancelRefundPickerModal
        isOpen={isCancelPickerOpen}
        bookings={todaysBookings}
        onClose={() => setIsCancelPickerOpen(false)}
        onPicked={(booking) => {
          setIsCancelPickerOpen(false)
          setCancelTarget(booking)
        }}
      />

      <CancelBookingModal
        booking={cancelTarget}
        onClose={() => setCancelTarget(null)}
        onCancelled={() => setCancelTarget(null)}
      />

      <NewWalkInModal
        isOpen={isWalkInModalOpen}
        onClose={() => setIsWalkInModalOpen(false)}
        onScheduled={() => {
          refetchTodaysBookings()
          setIsWalkInModalOpen(false)
        }}
      />

      <ReassignBookingModal
        isOpen={isReassignModalOpen}
        bookings={todaysBookings}
        onClose={() => setIsReassignModalOpen(false)}
        onReassigned={refetchTodaysBookings}
      />
    </div>
  )
}
