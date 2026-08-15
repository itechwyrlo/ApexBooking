import { Badge, type BadgeTone } from '../common/Badge'
import { formatStatusLabel } from '../../utils/statusLabel'
import { RefundRequestStatus } from '../../types/RefundRequestStatus'

interface IRefundRequestStatusBadgeProps {
  status: RefundRequestStatus
}

// Extracted from RefundRequestTable.tsx, which already followed the same tone/label pattern as
// every other domain status badge, just inline instead of its own file.
const STATUS_TONE: Record<RefundRequestStatus, BadgeTone> = {
  [RefundRequestStatus.PendingReview]: 'warning',
  [RefundRequestStatus.Refunded]: 'success',
  [RefundRequestStatus.Rejected]: 'danger',
}

export function RefundRequestStatusBadge({ status }: IRefundRequestStatusBadgeProps) {
  return <Badge tone={STATUS_TONE[status]}>{formatStatusLabel(status)}</Badge>
}
