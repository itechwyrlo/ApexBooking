import { useState } from 'react'
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'
import { AdmitScanModal } from '../../components/appointments/AdmitScanModal'
import { NewWalkInModal } from '../../components/appointments/NewWalkInModal'
import { ReassignBookingModal } from '../../components/dashboard/ReassignBookingModal'
import { MasterVisualGrid } from '../../components/dashboard/MasterVisualGrid'
import { useTenantBookingCounts } from '../../hooks/useTenantBookingCounts'
import { useIdleStaff } from '../../hooks/useIdleStaff'
import { useTenantBookings } from '../../hooks/useTenantBookings'
import { useActiveStaff } from '../../hooks/useActiveStaff'
import { BookingStatus } from '../../types/BookingStatus'

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

export function AdminDashboardPage() {
  const todayIso = getTodayIsoDate()
  const { counts, isLoading: isCountsLoading, refetch: refetchCounts } = useTenantBookingCounts(todayIso)
  const { staff: idleStaff, isLoading: isIdleStaffLoading } = useIdleStaff()
  const { bookings: todaysBookings } = useTenantBookings({ status: BookingStatus.Scheduled, fromDate: todayIso, toDate: todayIso })
  const { staff: activeStaff, isLoading: isActiveStaffLoading } = useActiveStaff()

  const [isScanModalOpen, setIsScanModalOpen] = useState(false)
  const [isWalkInModalOpen, setIsWalkInModalOpen] = useState(false)
  const [isReassignModalOpen, setIsReassignModalOpen] = useState(false)

  return (
    <div>
      <PageHeader title="Front Desk" description={TODAY_LABEL} />

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">Master Visual Grid</h2>
            <MasterVisualGrid staff={activeStaff} bookings={todaysBookings} isLoading={isActiveStaffLoading} />
          </Card>
        </div>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Daily Booking Counters</h2>
            {isCountsLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : !counts ? (
              <p className="text-muted small mb-0">Failed to load today's counts.</p>
            ) : (
              <div className="row g-2 text-center">
                <div className="col-3">
                  <p className="fs-4 fw-bold mb-0">{counts.pending}</p>
                  <p className="text-muted small mb-0">Pending</p>
                </div>
                <div className="col-3">
                  <p className="fs-4 fw-bold mb-0">{counts.checkedIn}</p>
                  <p className="text-muted small mb-0">Checked-In</p>
                </div>
                <div className="col-3">
                  <p className="fs-4 fw-bold mb-0">{counts.completed}</p>
                  <p className="text-muted small mb-0">Completed</p>
                </div>
                <div className="col-3">
                  <p className="fs-4 fw-bold mb-0">{counts.missed}</p>
                  <p className="text-muted small mb-0">Missed</p>
                </div>
              </div>
            )}
          </Card>
        </div>
        <div className="col-12 col-md-6">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Idle Staff</h2>
            {isIdleStaffLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : idleStaff.length === 0 ? (
              <EmptyState
                icon="alert-triangle"
                title="No idle staff"
                description="Every active team member is assigned to at least one service."
              />
            ) : (
              <ul className="list-unstyled mb-0">
                {idleStaff.map((member) => (
                  <li key={member.tenantMemberId} className="d-flex align-items-center gap-2 py-1">
                    <span className="fw-semibold small">{member.name}</span>
                    <span className="text-muted small">— not assigned to any service</span>
                  </li>
                ))}
              </ul>
            )}
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
            </div>
          </Card>
        </div>
      </div>

      <AdmitScanModal isOpen={isScanModalOpen} onClose={() => setIsScanModalOpen(false)} onAdmitted={refetchCounts} />
      <NewWalkInModal
        isOpen={isWalkInModalOpen}
        onClose={() => setIsWalkInModalOpen(false)}
        onScheduled={() => {
          refetchCounts()
          setIsWalkInModalOpen(false)
        }}
      />
      <ReassignBookingModal
        isOpen={isReassignModalOpen}
        bookings={todaysBookings}
        onClose={() => setIsReassignModalOpen(false)}
        onReassigned={refetchCounts}
      />
    </div>
  )
}
