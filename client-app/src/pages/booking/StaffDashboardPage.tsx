import { useState } from 'react'
import axios from 'axios'
import { Button } from '../../components/common/Button'
import { Card } from '../../components/common/Card'
import { EmptyState } from '../../components/common/EmptyState'
import { PageHeader } from '../../components/common/PageHeader'
import { StaffLineupTimeline } from '../../components/dashboard/StaffLineupTimeline'
import { SaveChairNotesModal } from '../../components/dashboard/SaveChairNotesModal'
import { BlockMyTimeModal } from '../../components/dashboard/BlockMyTimeModal'
import { useAuth } from '../../hooks/useAuth'
import { useTenantBookings } from '../../hooks/useTenantBookings'
import { useCustomerLatestNote } from '../../hooks/useCustomerLatestNote'
import { useToast } from '../../hooks/useToast'
import { setBookingStaffNotes } from '../../services/bookingService'
import { blockMyTime } from '../../services/timeOffService'
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

// The "active" appointment for the Client Preference View: whichever is currently checked in
// and in progress, or — if none is — the next upcoming one today. Neither existing means there's
// nothing to preview right now.
function findActiveBooking(bookings: ITenantBooking[]): ITenantBooking | null {
  const inProgress = bookings.find((b) => b.status === BookingStatus.Scheduled && b.checkedInAt !== null)
  if (inProgress) return inProgress

  const upcoming = bookings
    .filter((b) => b.status === BookingStatus.Scheduled && b.checkedInAt === null)
    .sort((a, b) => a.scheduledStartTime.localeCompare(b.scheduledStartTime))

  return upcoming[0] ?? null
}

export function StaffDashboardPage() {
  const { user } = useAuth()
  const { showToast } = useToast()
  const todayIso = getTodayIsoDate()
  const { bookings, isLoading } = useTenantBookings({
    staffId: user?.tenantMemberId ?? undefined,
    fromDate: todayIso,
    toDate: todayIso,
  })

  const activeBooking = findActiveBooking(bookings)
  const { note: latestNote, isLoading: isNoteLoading } = useCustomerLatestNote(activeBooking?.customerId ?? null)

  const [isChairNotesModalOpen, setIsChairNotesModalOpen] = useState(false)
  const [isSavingNotes, setIsSavingNotes] = useState(false)

  const handleSaveChairNotes = async (bookingId: string, notes: string) => {
    setIsSavingNotes(true)
    try {
      await setBookingStaffNotes(bookingId, notes)
      showToast('success', 'Chair notes saved.')
      setIsChairNotesModalOpen(false)
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to save chair notes. Please try again.')
    } finally {
      setIsSavingNotes(false)
    }
  }

  const [isBlockTimeModalOpen, setIsBlockTimeModalOpen] = useState(false)
  const [isBlockingTime, setIsBlockingTime] = useState(false)

  const handleBlockMyTime = async (startTime: string, endTime: string, reason: string) => {
    setIsBlockingTime(true)
    try {
      await blockMyTime(todayIso, startTime, endTime, reason)
      showToast('success', 'Your time has been blocked.')
      setIsBlockTimeModalOpen(false)
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to block your time. Please try again.')
    } finally {
      setIsBlockingTime(false)
    }
  }

  return (
    <div>
      <PageHeader title="My Day" description={TODAY_LABEL} />

      <div className="row g-3 mb-3">
        <div className="col-12">
          <Card>
            <h2 className="fs-6 fw-semibold mb-3">My Daily Lineup</h2>
            <StaffLineupTimeline bookings={bookings} isLoading={isLoading} />
          </Card>
        </div>
      </div>

      <div className="row g-3">
        <div className="col-12 col-lg-8">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Client Preferences</h2>
            {!activeBooking ? (
              <EmptyState
                icon="clients"
                title="No active appointment right now"
                description="Past service notes for your active client will preview here."
              />
            ) : isNoteLoading ? (
              <p className="text-muted small mb-0">Loading…</p>
            ) : latestNote ? (
              <div>
                <p className="fw-semibold mb-1">{activeBooking.customerName}</p>
                <p className="text-muted small mb-1">Last visit: {formatDisplayDate(latestNote.notedOn)}</p>
                <p className="mb-0">{latestNote.notes}</p>
              </div>
            ) : (
              <EmptyState
                icon="clients"
                title={`No notes yet for ${activeBooking.customerName}`}
                description="Chair notes from their next completed visit will preview here."
              />
            )}
          </Card>
        </div>
        <div className="col-12 col-lg-4">
          <Card className="h-100">
            <h2 className="fs-6 fw-semibold mb-3">Quick Tools</h2>
            <div className="d-flex flex-wrap gap-2">
              <Button variant="outline-secondary" size="sm" icon="time-offs" onClick={() => setIsBlockTimeModalOpen(true)}>
                Block My Time
              </Button>
              <Button variant="outline-secondary" size="sm" icon="edit" onClick={() => setIsChairNotesModalOpen(true)}>
                Save Chair Notes
              </Button>
            </div>
          </Card>
        </div>
      </div>

      <SaveChairNotesModal
        isOpen={isChairNotesModalOpen}
        bookings={bookings.filter((b) => b.status === BookingStatus.Completed)}
        isSubmitting={isSavingNotes}
        onClose={() => setIsChairNotesModalOpen(false)}
        onSubmit={handleSaveChairNotes}
      />

      <BlockMyTimeModal
        isOpen={isBlockTimeModalOpen}
        date={todayIso}
        isSubmitting={isBlockingTime}
        onClose={() => setIsBlockTimeModalOpen(false)}
        onSubmit={handleBlockMyTime}
      />
    </div>
  )
}
