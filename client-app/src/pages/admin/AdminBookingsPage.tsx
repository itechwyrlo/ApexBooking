import { useState } from 'react'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { Pagination } from '../../components/common/Pagination'
import { EmptyState } from '../../components/common/EmptyState'
import { TableSkeleton } from '../../components/common/TableSkeleton'
import { BookingStatusBadge } from '../../components/admin/BookingStatusBadge'
import { usePlatformBookings } from '../../hooks/usePlatformBookings'
import { BookingStatus } from '../../types/BookingStatus'
import { formatDisplayDate, formatDisplayTime } from '../../utils/formatDateTime'
import type { IPlatformBookingFilters } from '../../interfaces/IPlatformBooking'

const COLUMNS = ['Reference', 'Tenant', 'Customer', 'Team Member', 'Service', 'Date', 'Time', 'Status']
const PAGE_SIZE = 10

export function AdminBookingsPage() {
  const [filters, setFilters] = useState<IPlatformBookingFilters>({})
  const [pageNumber, setPageNumber] = useState(1)

  const { bookings, total, isLoading, error } = usePlatformBookings(filters, { pageNumber, pageSize: PAGE_SIZE })

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const handleFilterChange = (next: IPlatformBookingFilters) => {
    setFilters(next)
    setPageNumber(1)
  }

  return (
    <div>
      <PageHeader title="Bookings" description="Cross-tenant view of every appointment on the platform." />

      <Card className="mb-3">
        <div className="row g-3 align-items-end">
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted" htmlFor="status-filter">
              Status
            </label>
            <select
              id="status-filter"
              className="form-select"
              value={filters.status ?? ''}
              onChange={(e) =>
                handleFilterChange({ ...filters, status: (e.target.value || undefined) as IPlatformBookingFilters['status'] })
              }
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
            <label className="form-label small text-muted" htmlFor="from-date">
              From
            </label>
            <input
              id="from-date"
              type="date"
              className="form-control"
              value={filters.fromDate ?? ''}
              onChange={(e) => handleFilterChange({ ...filters, fromDate: e.target.value || undefined })}
            />
          </div>
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted" htmlFor="to-date">
              To
            </label>
            <input
              id="to-date"
              type="date"
              className="form-control"
              value={filters.toDate ?? ''}
              onChange={(e) => handleFilterChange({ ...filters, toDate: e.target.value || undefined })}
            />
          </div>
          <div className="col-6 col-md-3">
            <Button variant="outline-secondary" size="sm" onClick={() => handleFilterChange({})}>
              Clear filters
            </Button>
          </div>
        </div>
      </Card>

      <Card>
        {error && <p className="text-danger small mb-3">{error}</p>}

        {isLoading ? (
          <TableSkeleton columns={COLUMNS.length} rows={5} />
        ) : bookings.length === 0 ? (
          <EmptyState
            icon="appointments"
            title="No bookings found."
            description="Try adjusting the filters, or check back once bookings come in."
          />
        ) : (
          <div className="table-responsive">
            <table className="table table-stack align-middle mb-0">
              <thead>
                <tr className="text-muted small text-uppercase">
                  {COLUMNS.map((column) => (
                    <th key={column} scope="col" className="fw-semibold">
                      {column}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {bookings.map((booking) => (
                  <tr key={booking.bookingId}>
                    <td className="fw-semibold" data-label="Reference">
                      {booking.bookingReference}
                    </td>
                    <td data-label="Tenant">{booking.tenantName}</td>
                    <td data-label="Customer">{booking.customerName}</td>
                    <td data-label="Team Member">{booking.staffName}</td>
                    <td data-label="Service">{booking.serviceName}</td>
                    <td data-label="Date">{formatDisplayDate(booking.scheduledDate)}</td>
                    <td data-label="Time">{formatDisplayTime(booking.scheduledStartTime)}</td>
                    <td data-label="Status">
                      <BookingStatusBadge status={booking.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {!isLoading && total > 0 && (
          <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <p className="text-muted small mb-0">
              Page {pageNumber} of {totalPages} ({total} bookings)
            </p>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
          </div>
        )}
      </Card>
    </div>
  )
}
