import { RowActions } from '../common/RowActions'
import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RefundRequestStatusBadge } from './RefundRequestStatusBadge'
import { formatRelativeTime } from '../../utils/formatDateTime'
import { RefundRequestStatus } from '../../types/RefundRequestStatus'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

const COLUMNS = ['Booking', 'Customer', 'Amount', 'Amount Paid', 'PayMongo Ref.', 'E-Wallet', 'Status', 'Due', '']

function isDueSoon(dueDate: string): boolean {
  const diffMs = new Date(dueDate).getTime() - Date.now()
  return diffMs <= 2 * 24 * 60 * 60 * 1000
}

interface IRefundRequestTableProps {
  requests: IRefundRequest[]
  isLoading?: boolean
  busyId: string | null
  onConfirm: (request: IRefundRequest) => void
  onReject: (request: IRefundRequest) => void
}

export function RefundRequestTable({ requests, isLoading, busyId, onConfirm, onReject }: IRefundRequestTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (requests.length === 0) {
    return <EmptyState icon="refund" title="No refunds need review right now." description="Cancelled bookings eligible for a refund will show up here." />
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
          {requests.map((request) => {
            const isBusy = busyId === request.id
            const actions =
              request.status === RefundRequestStatus.PendingReview
                ? [
                    { label: 'Confirm', icon: 'check-circle', tone: 'primary' as const, onClick: () => onConfirm(request) },
                    { label: 'Reject', icon: 'x-circle', tone: 'delete' as const, onClick: () => onReject(request) },
                  ]
                : []

            return (
              <tr key={request.id}>
                <td className="fw-semibold" data-label="Booking">{request.bookingReference}</td>
                <td data-label="Customer">{request.customerName}</td>
                <td data-label="Amount">{request.requestedAmount.toFixed(2)} {request.currencyCode}</td>
                <td data-label="Amount Paid">
                  <span className="text-muted small">{request.amountPaid.toFixed(2)} {request.currencyCode}</span>
                </td>
                <td data-label="PayMongo Ref.">
                  {request.payMongoPaymentId ? (
                    <span className="text-mono small text-truncate d-inline-block table-cell-truncate" title={request.payMongoPaymentId}>
                      {request.payMongoPaymentId}
                    </span>
                  ) : (
                    <span className="text-muted">—</span>
                  )}
                </td>
                <td data-label="E-Wallet">
                  <span className="small">
                    {request.customerEwalletProvider}: <span className="fw-semibold">{request.customerEwalletNumber}</span>
                    <span className="text-muted"> ({request.customerEwalletName})</span>
                  </span>
                </td>
                <td data-label="Status">
                  <RefundRequestStatusBadge status={request.status} />
                  {request.status === RefundRequestStatus.Refunded && request.receiptUrl && (
                    <a href={request.receiptUrl} target="_blank" rel="noreferrer" className="d-block small mt-1">
                      View receipt
                    </a>
                  )}
                </td>
                <td data-label="Due">
                  <span className={request.status === RefundRequestStatus.PendingReview && isDueSoon(request.dueDate) ? 'text-danger fw-semibold' : 'text-muted'}>
                    {formatRelativeTime(request.dueDate)}
                  </span>
                </td>
                <td className="text-end" data-label="Actions">
                  <RowActions
                    actions={actions.map((action) => ({
                      ...action,
                      disabled: isBusy,
                      isLoading: isBusy,
                    }))}
                    forceMenu={actions.length > 1}
                  />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
