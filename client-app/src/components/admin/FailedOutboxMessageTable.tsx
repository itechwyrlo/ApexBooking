import { Badge } from '../common/Badge'
import { RowActions } from '../common/RowActions'
import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { formatRelativeTime } from '../../utils/formatDateTime'
import type { IFailedOutboxMessage } from '../../interfaces/IFailedOutboxMessage'

const COLUMNS = ['Event', 'Error', 'Retries', 'Occurred', 'Actions']

// "ApexBooking.Core.Domain.Events.BookingCancelledDomainEvent" -> "Booking Cancelled"
function formatEventType(eventType: string): string {
  const shortName = eventType.split('.').pop() ?? eventType
  const withoutSuffix = shortName.replace(/DomainEvent$/, '')
  return withoutSuffix.replace(/([a-z])([A-Z])/g, '$1 $2')
}

interface IFailedOutboxMessageTableProps {
  messages: IFailedOutboxMessage[]
  isLoading?: boolean
  retryingId: string | null
  onRetry: (message: IFailedOutboxMessage) => void
}

export function FailedOutboxMessageTable({ messages, isLoading, retryingId, onRetry }: IFailedOutboxMessageTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (messages.length === 0) {
    return (
      <EmptyState
        icon="check-circle"
        title="No failed notifications."
        description="Every notification and email delivery has gone through successfully."
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
          {messages.map((message) => (
            <tr key={message.id}>
              <td className="fw-semibold" data-label="Event">
                {formatEventType(message.eventType)}
              </td>
              <td data-label="Error">
                <span className="text-danger text-break">{message.lastError ?? '—'}</span>
              </td>
              <td data-label="Retries">
                <Badge tone="danger">{message.retryCount} attempts</Badge>
              </td>
              <td data-label="Occurred">{formatRelativeTime(message.occurredAtUtc)}</td>
              <td data-label="Actions">
                <RowActions
                  actions={[
                    {
                      label: 'Retry',
                      icon: 'refresh',
                      tone: 'primary',
                      isLoading: retryingId === message.id,
                      disabled: retryingId !== null && retryingId !== message.id,
                      onClick: () => onRetry(message),
                    },
                  ]}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
