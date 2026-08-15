import { Badge, type BadgeTone } from '../common/Badge'
import { formatStatusLabel } from '../../utils/statusLabel'
import { TenantMemberStatus } from '../../types/TenantMemberStatus'

interface ITeamStatusBadgeProps {
  status: TenantMemberStatus
}

const STATUS_TONE: Record<TenantMemberStatus, BadgeTone> = {
  [TenantMemberStatus.Invited]: 'warning',
  [TenantMemberStatus.Active]: 'success',
  [TenantMemberStatus.Deactivated]: 'danger',
}

export function TeamStatusBadge({ status }: ITeamStatusBadgeProps) {
  return <Badge tone={STATUS_TONE[status]}>{formatStatusLabel(status)}</Badge>
}
