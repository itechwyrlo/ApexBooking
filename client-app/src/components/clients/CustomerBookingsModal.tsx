import { useState } from 'react'
import { Modal } from '../common/Modal'
import { Pagination } from '../common/Pagination'
import { TableSkeleton } from '../common/TableSkeleton'
import { EmptyState } from '../common/EmptyState'
import { BookingStatusBadge } from '../admin/BookingStatusBadge'
import { useCustomerBookings } from '../../hooks/useCustomerBookings'
import { formatMoney } from '../../utils/formatMoney'
import { BookingStatus } from '../../types/BookingStatus'
import { PaymentConfirmationMethod } from '../../types/PaymentConfirmationMethod'
import type { ICustomerBooking } from '../../interfaces/ICustomerBooking'

const PAGE_SIZE = 10

function formatDateTime(date: string, time: string): string {
  const parsed = new Date(`${date}T${time}`)
  return parsed.toLocaleString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' })
}

function PaymentDetail({ booking }: { booking: ICustomerBooking }) {
  const amount = formatMoney(booking.amountDue, booking.currencyCode)

  if (!booking.paymentConfirmedVia) {
    const label = booking.status === BookingStatus.PendingPayment ? 'awaiting payment' : 'pay in visit'
    return <span className="text-warning small">{amount} — {label}</span>
  }

  const via = booking.paymentConfirmedVia === PaymentConfirmationMethod.Online ? 'paid online' : 'paid in visit'
  return (
    <span className="small">
      {amount} — <span className="text-success">{via}</span>
    </span>
  )
}

interface ICustomerBookingsModalProps {
  isOpen: boolean
  customerId: string | null
  customerName: string
  onClose: () => void
}

export function CustomerBookingsModal({ isOpen, customerId, customerName, onClose }: ICustomerBookingsModalProps) {
  const [pageNumber, setPageNumber] = useState(1)
  const { bookings, total, isLoading } = useCustomerBookings(customerId, { pageNumber, pageSize: PAGE_SIZE })

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <Modal isOpen={isOpen} title={`Booking History — ${customerName}`} onClose={onClose}>
      {isLoading ? (
        <TableSkeleton columns={4} rows={4} />
      ) : bookings.length === 0 ? (
        <EmptyState icon="appointments" title="No bookings yet" description="This client hasn't made any bookings." />
      ) : (
        <>
          <div className="table-responsive">
            <table className="table table-stack align-middle mb-0">
              <thead>
                <tr className="text-muted small text-uppercase">
                  <th scope="col" className="fw-semibold">
                    Service
                  </th>
                  <th scope="col" className="fw-semibold">
                    When
                  </th>
                  <th scope="col" className="fw-semibold">
                    Status
                  </th>
                  <th scope="col" className="fw-semibold">
                    Payment
                  </th>
                </tr>
              </thead>
              <tbody>
                {bookings.map((booking) => (
                  <tr key={booking.bookingId}>
                    <td data-label="Service">
                      <div className="fw-semibold">{booking.serviceName}</div>
                      <div className="text-muted small">
                        {booking.staffName} · {booking.branchName}
                      </div>
                    </td>
                    <td data-label="When">{formatDateTime(booking.scheduledDate, booking.scheduledStartTime)}</td>
                    <td data-label="Status">
                      <BookingStatusBadge status={booking.status} />
                    </td>
                    <td data-label="Payment">
                      <PaymentDetail booking={booking} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
              <p className="text-muted small mb-0">
                Page {pageNumber} of {totalPages}
              </p>
              <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
            </div>
          )}
        </>
      )}
    </Modal>
  )
}
