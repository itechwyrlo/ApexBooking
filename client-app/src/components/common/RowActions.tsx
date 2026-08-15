import { Button } from './Button'
import { Icon } from './Icon'

/** edit = green, delete = red, view = gray, primary = other positive workflow actions (approve, admit, retry, etc). */
export type RowActionTone = 'edit' | 'delete' | 'view' | 'primary'

export interface IRowAction {
  label: string
  icon: string
  tone: RowActionTone
  onClick: () => void
  disabled?: boolean
  isLoading?: boolean
}

const TONE_CHIP_CLASS: Record<RowActionTone, string> = {
  edit: 'action-icon-edit',
  delete: 'action-icon-delete',
  view: 'action-icon-view',
  primary: 'action-icon-primary',
}

const TONE_TEXT_CLASS: Record<RowActionTone, string> = {
  edit: 'text-success',
  delete: 'text-danger',
  view: 'text-muted',
  primary: 'text-primary',
}

interface IRowActionsProps {
  actions: IRowAction[]
  /** Opt-in per table, not a global change to the default threshold below. Forces the overflow
   * menu even at just 2 actions — for a table whose 2-action case is unusually high-stakes/dense
   * rather than routine (RefundRequestTable: financial approve/reject decisions on its own
   * densest-in-the-app row), where the extra deliberate tap is wanted even though the same count
   * stays direct everywhere else (TeamMemberTable's Edit+Remove, TimeOffTable's Approve+Reject). */
  forceMenu?: boolean
}

export function RowActions({ actions, forceMenu }: IRowActionsProps) {
  if (actions.length === 0) return null

  // Default: only "several" actions (3+) collapse into overflow — matching the app's own
  // row-action vocabulary, every real table tops out at exactly 3. A destructive action alone no
  // longer forces the menu on its own: at 1-2 actions, the common action(s) stay directly visible
  // (never hidden behind a tap just because a sibling happens to be destructive), and the
  // destructive one is still visually distinguished by its existing tone chip (soft red at rest,
  // solid on hover) — distinguishable without being dominant, not hidden.
  const useMenu = forceMenu || actions.length >= 3

  if (!useMenu) {
    return (
      <div className="table-actions">
        {actions.map((action) => (
          <Button
            key={action.label}
            variant="outline-secondary"
            size="sm"
            icon={action.icon}
            iconOnly
            aria-label={action.label}
            disabled={action.disabled}
            isLoading={action.isLoading}
            onClick={action.onClick}
            className={`action-icon row-action-btn ${TONE_CHIP_CLASS[action.tone]}`}
          >
            {action.label}
          </Button>
        ))}
      </div>
    )
  }

  let dividerInserted = false

  return (
    <div className="dropdown d-inline-block">
      <button
        type="button"
        className="btn btn-outline-secondary btn-sm btn-icon row-action-btn"
        data-bs-toggle="dropdown"
        aria-expanded="false"
        aria-label="More actions"
        title="More actions"
      >
        <Icon name="more-horizontal" size={16} />
      </button>
      <ul className="dropdown-menu dropdown-menu-end">
        {actions.map((action) => {
          const showDivider = action.tone === 'delete' && !dividerInserted
          if (showDivider) dividerInserted = true

          return (
            <li key={action.label}>
              {showDivider && <hr className="dropdown-divider" />}
              <button
                type="button"
                className={`dropdown-item d-flex align-items-center gap-2 ${TONE_TEXT_CLASS[action.tone]}`}
                disabled={action.disabled || action.isLoading}
                onClick={action.onClick}
              >
                <Icon name={action.icon} size={16} />
                {action.label}
              </button>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
