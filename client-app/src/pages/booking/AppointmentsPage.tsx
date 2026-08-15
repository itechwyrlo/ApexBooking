import { useState } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { Icon } from '../../components/common/Icon'
import { Pagination } from '../../components/common/Pagination'
import { BookingTable } from '../../components/appointments/BookingTable'
import { CancelBookingModal } from '../../components/appointments/CancelBookingModal'
import { AdmitScanModal } from '../../components/appointments/AdmitScanModal'
import { NewWalkInModal } from '../../components/appointments/NewWalkInModal'
import { useTenantBookings } from '../../hooks/useTenantBookings'
import { useBranches } from '../../hooks/useBranches'
import { useToast } from '../../hooks/useToast'
import { admitBooking, completeBooking, markBookingNoShow } from '../../services/bookingService'
import { BookingStatus } from '../../types/BookingStatus'
import type { ITenantBooking, ITenantBookingFilters } from '../../interfaces/ITenantBooking'

const PAGE_SIZE = 10

function getTodayIsoDate(): string {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

export function AppointmentsPage() {
  const [filters, setFilters] = useState<ITenantBookingFilters>(() => ({ fromDate: getTodayIsoDate(), toDate: getTodayIsoDate() }))
  const [pageNumber, setPageNumber] = useState(1)
  const [cancelTarget, setCancelTarget] = useState<ITenantBooking | null>(null)
  const [isScanOpen, setIsScanOpen] = useState(false)
  const [isWalkInOpen, setIsWalkInOpen] = useState(false)
  const [pendingBookingId, setPendingBookingId] = useState<string | null>(null)

  const { branches } = useBranches()
  const { bookings, total, isLoading, error, refetch } = useTenantBookings(filters, { pageNumber, pageSize: PAGE_SIZE })
  const { showToast } = useToast()

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))
  const activeFilterCount = [filters.branchId, filters.status, filters.fromDate].filter(Boolean).length

  const handleFilterChange = (next: ITenantBookingFilters) => {
    setFilters(next)
    setPageNumber(1)
  }

  const runAction = async (booking: ITenantBooking, action: () => Promise<void>, successMessage: string) => {
    setPendingBookingId(booking.bookingId)
    try {
      await action()
      showToast('success', successMessage)
      refetch()
    } catch (actionError) {
      const detail = axios.isAxiosError(actionError)
        ? (actionError.response?.data as { detail?: string } | undefined)?.detail
        : undefined
      showToast('error', detail ?? 'That action could not be completed. Please try again.')
    } finally {
      setPendingBookingId(null)
    }
  }

  const handleAdmit = async (booking: ITenantBooking) => {
    setPendingBookingId(booking.bookingId)
    try {
      const result = await admitBooking(booking.bookingId)
      if (result.wasFirstAdmission) {
        showToast('success', `${booking.bookingReference} admitted.`)
      } else {
        showToast('info', `${booking.bookingReference} is already admitted.`)
      }
      refetch()
    } catch (actionError) {
      const detail = axios.isAxiosError(actionError)
        ? (actionError.response?.data as { detail?: string } | undefined)?.detail
        : undefined
      showToast('error', detail ?? 'That action could not be completed. Please try again.')
    } finally {
      setPendingBookingId(null)
    }
  }

  const handleComplete = (booking: ITenantBooking) =>
    runAction(booking, () => completeBooking(booking.bookingId), `${booking.bookingReference} marked as completed.`)

  const handleNoShow = (booking: ITenantBooking) =>
    runAction(booking, () => markBookingNoShow(booking.bookingId), `${booking.bookingReference} marked as no-show.`)

  return (
    <div>
      <PageHeader
        title="Appointments"
        description="Manage today's bookings, admit arrivals, and create walk-in appointments."
        secondaryAction={
          <button
            type="button"
            className="btn btn-outline-secondary icon-tooltip d-inline-flex align-items-center justify-content-center px-3"
            data-tooltip="Scan Boarding Pass"
            aria-label="Scan Boarding Pass"
            onClick={() => setIsScanOpen(true)}
          >
            <Icon name="qr-code" size={20} />
          </button>
        }
        primaryAction={<Button onClick={() => setIsWalkInOpen(true)}>New Walk-in</Button>}
      />

      <Card className="mb-3">
        <div className="row g-3 align-items-end">
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted" htmlFor="branch-filter">
              Branch
            </label>
            <select
              id="branch-filter"
              className="form-select"
              value={filters.branchId ?? ''}
              onChange={(e) => handleFilterChange({ ...filters, branchId: e.target.value || undefined })}
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
            <label className="form-label small text-muted" htmlFor="status-filter">
              Status
            </label>
            <select
              id="status-filter"
              className="form-select"
              value={filters.status ?? ''}
              onChange={(e) => handleFilterChange({ ...filters, status: (e.target.value || undefined) as ITenantBookingFilters['status'] })}
            >
              <option value="">All statuses</option>
              {Object.values(BookingStatus).map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted" htmlFor="date-filter">
              Date
            </label>
            <input
              id="date-filter"
              type="date"
              className="form-control"
              value={filters.fromDate ?? ''}
              onChange={(e) => handleFilterChange({ ...filters, fromDate: e.target.value || undefined, toDate: e.target.value || undefined })}
            />
          </div>
          <div className="col-6 col-md-3">
            <Button
              variant="outline-secondary"
              size="sm"
              disabled={activeFilterCount === 0}
              onClick={() => handleFilterChange({})}
            >
              Clear Filters{activeFilterCount > 0 ? ` (${activeFilterCount})` : ''}
            </Button>
          </div>
        </div>
      </Card>

      <Card>
        {error && <p className="text-danger small mb-3">{error}</p>}

        <BookingTable
          bookings={bookings}
          isLoading={isLoading}
          pendingBookingId={pendingBookingId}
          onAdmit={handleAdmit}
          onComplete={handleComplete}
          onCancel={setCancelTarget}
          onNoShow={handleNoShow}
        />

        {!isLoading && total > 0 && (
          <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <p className="text-muted small mb-0">
              Page {pageNumber} of {totalPages} ({total} appointments)
            </p>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
          </div>
        )}
      </Card>

      <CancelBookingModal booking={cancelTarget} onClose={() => setCancelTarget(null)} onCancelled={refetch} />
      <AdmitScanModal isOpen={isScanOpen} onClose={() => setIsScanOpen(false)} onAdmitted={refetch} />
      <NewWalkInModal isOpen={isWalkInOpen} onClose={() => setIsWalkInOpen(false)} onScheduled={refetch} />
    </div>
  )
}
