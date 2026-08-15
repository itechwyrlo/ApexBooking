import { Badge } from './Badge'

interface IActiveStatusBadgeProps {
  isActive: boolean
}

// The identical `isActive ? success/'Active' : neutral/'Inactive'` badge existed verbatim in both
// ServiceTable.tsx (twice — desktop table + mobile card) and BranchTable.tsx before this
// consolidation. Not an enum-backed domain status like the others, just a real, repeated
// boolean-flag pattern worth sharing once it showed up in more than one place.
export function ActiveStatusBadge({ isActive }: IActiveStatusBadgeProps) {
  return <Badge tone={isActive ? 'success' : 'neutral'}>{isActive ? 'Active' : 'Inactive'}</Badge>
}
