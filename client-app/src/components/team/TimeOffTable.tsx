import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RowActions } from '../common/RowActions'
import { TimeOffStatusBadge } from './TimeOffStatusBadge'
import { TimeOffType } from '../../types/TimeOffType'
import { TimeOffStatus } from '../../types/TimeOffStatus'
import { formatDisplayDate, formatDisplayTime } from '../../utils/formatDateTime'
import type { ITimeOffRequest } from '../../interfaces/ITimeOffRequest'

function formatDateRange(startDate: string, endDate: string): string {
  return startDate === endDate
    ? formatDisplayDate(startDate)
    : `${formatDisplayDate(startDate)} – ${formatDisplayDate(endDate)}`
}

function formatTimeRange(startTime: string | null, endTime: string | null): string | null {
  if (!startTime || !endTime) return null
  return `${formatDisplayTime(startTime)} – ${formatDisplayTime(endTime)}`
}

interface ITimeOffTableProps {
  requests: ITimeOffRequest[]
  isLoading?: boolean
  showMemberColumn: boolean
  canReview: boolean
  pendingRequestId: string | null
  onApprove: (request: ITimeOffRequest) => void
  onReject: (request: ITimeOffRequest) => void
}

export function TimeOffTable({ requests, isLoading, showMemberColumn, canReview, pendingRequestId, onApprove, onReject }: ITimeOffTableProps) {
  const columns = [
    ...(showMemberColumn ? ['Team Member'] : []),
    'Dates',
    'Type',
    'Reason',
    'Status',
    ...(canReview ? [''] : []),
  ]

  if (isLoading) {
    return <TableSkeleton columns={columns.length} rows={5} />
  }

  if (requests.length === 0) {
    return (
      <EmptyState
        icon="time-offs"
        title="No time off requests"
        description={showMemberColumn ? 'Requests from your team will appear here.' : 'Requests you submit will appear here.'}
      />
    )
  }

  return (
    <div className="table-responsive">
      <table className="table table-stack align-middle mb-0">
        <thead>
          <tr className="text-muted small text-uppercase">
            {columns.map((column) => (
              <th key={column} scope="col" className="fw-semibold">
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {requests.map((request) => {
            const isPending = pendingRequestId === request.requestId
            const isReviewable = canReview && request.status === TimeOffStatus.Requested
            const timeRange = formatTimeRange(request.startTime, request.endTime)

            return (
              <tr key={request.requestId}>
                {showMemberColumn && <td data-label="Team Member" className="fw-semibold">{request.memberName}</td>}
                <td data-label="Dates">{formatDateRange(request.startDate, request.endDate)}</td>
                <td data-label="Type">
                  {request.type === TimeOffType.FullDay ? 'Full day' : 'Partial day'}
                  {timeRange && <div className="text-muted small">{timeRange}</div>}
                </td>
                <td data-label="Reason">{request.reason || <span className="text-muted">—</span>}</td>
                <td data-label="Status">
                  <TimeOffStatusBadge status={request.status} />
                </td>
                {canReview && (
                  <td className="text-end" data-label="Actions">
                    {isReviewable && (
                      <RowActions
                        actions={[
                          { label: 'Approve', icon: 'check-circle', tone: 'primary', disabled: isPending, onClick: () => onApprove(request) },
                          { label: 'Reject', icon: 'x-circle', tone: 'delete', disabled: isPending, onClick: () => onReject(request) },
                        ]}
                      />
                    )}
                  </td>
                )}
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
