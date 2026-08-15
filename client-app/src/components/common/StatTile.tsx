import type { ReactNode } from 'react'
import { Card } from './Card'
import { Icon } from './Icon'
import type { BadgeTone } from './Badge'

interface IStatTileProps {
  label: string
  value: number | string
  icon: string
  tone?: BadgeTone
  /** Reserved for a future TrendPill once a backend field provides a real prior-period value —
   * intentionally unused by every dashboard as of Step 6 (no dashboard DTO has one yet; see
   * docs/superpowers/plans/2026-08-14-step1-design-system-adjustments.md). */
  trend?: ReactNode
  /** Gives this tile a tone-tinted background + bolder value — for the one "headline" metric in a
   * related group (e.g. Total Revenue among Online/Pay-on-Visit). Size, radius, border, and grid
   * position stay identical to its siblings, so the group still reads as one card family, not a
   * separate, unrelated card. */
  emphasized?: boolean
}

// Generalized from the former components/admin/AdminSummaryCard.tsx (Super Admin's own metric
// tiles) — same shape, shared across every dashboard now instead of being admin-only.
export function StatTile({ label, value, icon, tone = 'neutral', trend, emphasized }: IStatTileProps) {
  return (
    <Card className={['h-100', emphasized ? 'stat-tile-emphasized' : ''].filter(Boolean).join(' ')}>
      <div className="d-flex align-items-center gap-2 mb-2">
        <span
          className={`stat-tile-icon-chip d-inline-flex align-items-center justify-content-center rounded-circle badge-tone-${tone}`}
          style={{ width: 36, height: 36 }}
          aria-hidden="true"
        >
          <Icon name={icon} size={18} />
        </span>
        <p className="text-muted small mb-0">{label}</p>
      </div>
      <div className="d-flex align-items-center gap-2">
        <p className="stat-tile-value fs-3 fw-bold mb-0">{value}</p>
        {trend}
      </div>
    </Card>
  )
}
