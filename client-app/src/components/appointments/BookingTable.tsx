import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RowActions, type IRowAction } from '../common/RowActions'
import { BookingStatusBadge } from '../admin/BookingStatusBadge'
import { BookingStatus } from '../../types/BookingStatus'
import { formatDisplayDate, formatDisplayTime } from '../../utils/formatDateTime'
import { useAuth } from '../../hooks/useAuth'
import { Role } from '../../types/Role'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

const COLUMNS = ['Reference', 'Customer', 'Service', 'Team Member', 'Branch', 'Date', 'Time', 'Status', '']

interface IBookingTableProps {
  bookings: ITenantBooking[]
  isLoading?: boolean
  pendingBookingId: string | null
  onAdmit: (booking: ITenantBooking) => void
  onComplete: (booking: ITenantBooking) => void
  onCancel: (booking: ITenantBooking) => void
  onNoShow: (booking: ITenantBooking) => void
}

export function BookingTable({ bookings, isLoading, pendingBookingId, onAdmit, onComplete, onCancel, onNoShow }: IBookingTableProps) {
  const { user } = useAuth()
  const canManage = (user?.roles ?? []).some((role) => role === Role.Owner || role === Role.Admin)

  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (bookings.length === 0) {
    return (
      <EmptyState
        icon="appointments"
        title="No appointments found."
        description="Try adjusting the filters, or check back once bookings come in."
      />
    )
  }

  return (
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
          {bookings.map((booking) => {
            const isPending = pendingBookingId === booking.bookingId
            const isScheduled = booking.status === BookingStatus.Scheduled
            const isCheckedIn = booking.checkedInAt !== null

            const actions: IRowAction[] = []
            if (isScheduled && !isCheckedIn) {
              actions.push({ label: 'Admit', icon: 'user-check', tone: 'primary', disabled: isPending, onClick: () => onAdmit(booking) })
              if (canManage) {
                actions.push({ label: 'No-show', icon: 'no-shows', tone: 'delete', disabled: isPending, onClick: () => onNoShow(booking) })
              }
              actions.push({ label: 'Cancel', icon: 'x-circle', tone: 'delete', disabled: isPending, onClick: () => onCancel(booking) })
            } else if (isScheduled && isCheckedIn) {
              if (canManage) {
                actions.push({ label: 'Complete', icon: 'check-circle', tone: 'primary', disabled: isPending, onClick: () => onComplete(booking) })
              }
              actions.push({ label: 'Cancel', icon: 'x-circle', tone: 'delete', disabled: isPending, onClick: () => onCancel(booking) })
            }

            return (
              <tr key={booking.bookingId}>
                <td className="fw-semibold" data-label="Reference">
                  {booking.bookingReference}
                </td>
                <td data-label="Customer">
                  <div>{booking.customerName}</div>
                  {booking.customerPhone && <div className="text-muted small">{booking.customerPhone}</div>}
                </td>
                <td data-label="Service">{booking.serviceName}</td>
                <td data-label="Team Member">{booking.staffName}</td>
                <td data-label="Branch">{booking.branchName}</td>
                <td data-label="Date">{formatDisplayDate(booking.scheduledDate)}</td>
                <td data-label="Time">{formatDisplayTime(booking.scheduledStartTime)}</td>
                <td data-label="Status">
                  <BookingStatusBadge status={booking.status} />
                  {isScheduled && isCheckedIn && <div className="text-muted small mt-1">Checked in</div>}
                </td>
                <td className="text-end" data-label="Actions">
                  <RowActions actions={actions} />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
