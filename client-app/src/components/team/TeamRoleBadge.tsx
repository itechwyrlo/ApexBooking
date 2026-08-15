import { Badge, type BadgeTone } from '../common/Badge'
import { Role } from '../../types/Role'

interface ITeamRoleBadgeProps {
  role: Role
}

// Roles are not statuses (no "success"/"failure" outcome semantics) — kept as its own small
// component with its own mapping rather than folded into the status-tone system, per role/status
// being genuinely different concepts here. Shares the same visual foundation (Badge) and the same
// BadgeTone type as every status badge, which is the level the two should be shared at.
const ROLE_TONE: Record<Role, BadgeTone> = {
  [Role.Owner]: 'primary',
  [Role.Admin]: 'success',
  [Role.Staff]: 'neutral',
}

export function TeamRoleBadge({ role }: ITeamRoleBadgeProps) {
  return <Badge tone={ROLE_TONE[role]}>{role}</Badge>
}
