import { Badge, type BadgeTone } from '../common/Badge'
import { formatStatusLabel } from '../../utils/statusLabel'
import { TimeOffStatus } from '../../types/TimeOffStatus'

interface ITimeOffStatusBadgeProps {
  status: TimeOffStatus
}

const STATUS_TONE: Record<TimeOffStatus, BadgeTone> = {
  [TimeOffStatus.Requested]: 'warning',
  [TimeOffStatus.Approved]: 'success',
  [TimeOffStatus.Rejected]: 'danger',
}

export function TimeOffStatusBadge({ status }: ITimeOffStatusBadgeProps) {
  return <Badge tone={STATUS_TONE[status]}>{formatStatusLabel(status)}</Badge>
}
