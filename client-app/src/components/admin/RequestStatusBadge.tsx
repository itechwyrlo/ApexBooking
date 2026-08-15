import { Badge, type BadgeTone } from '../common/Badge'
import { formatStatusLabel } from '../../utils/statusLabel'
import { TenantRequestStatus } from '../../types/TenantRequestStatus'

interface IRequestStatusBadgeProps {
  status: TenantRequestStatus
}

const STATUS_TONE: Record<TenantRequestStatus, BadgeTone> = {
  [TenantRequestStatus.Pending]: 'warning',
  [TenantRequestStatus.Approved]: 'success',
  [TenantRequestStatus.Rejected]: 'danger',
}

export function RequestStatusBadge({ status }: IRequestStatusBadgeProps) {
  return <Badge tone={STATUS_TONE[status]}>{formatStatusLabel(status)}</Badge>
}
