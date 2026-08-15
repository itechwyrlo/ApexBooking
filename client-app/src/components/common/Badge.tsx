import type { ReactNode } from 'react'

// Exported so every status/role badge (BookingStatusBadge, TeamRoleBadge, etc.) imports this one
// definition instead of each re-declaring the same union locally — previously duplicated across
// 5+ files, several of them missing 'teal' since they'd drifted from Badge's own type over time.
export type BadgeTone = 'neutral' | 'primary' | 'success' | 'warning' | 'danger' | 'teal'

interface IBadgeProps {
  tone?: BadgeTone
  className?: string
  children: ReactNode
}

export function Badge({ tone = 'neutral', className = '', children }: IBadgeProps) {
  return <span className={`badge rounded-pill fw-medium badge-tone-${tone} ${className}`.trim()}>{children}</span>
}
