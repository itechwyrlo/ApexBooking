import type { IPublicBranch } from '../../interfaces/publicBooking/IPublicBranch'
import type { WizardDirection } from '../../hooks/usePublicBookingWizard'
import { Icon } from '../common/Icon'

interface IBranchStepProps {
  branches: IPublicBranch[]
  selectedBranchId: string | null
  direction: WizardDirection
  onSelect: (branch: IPublicBranch) => void
}

export function BranchStep({ branches, selectedBranchId, direction, onSelect }: IBranchStepProps) {
  return (
    <div className={`pb-step-enter-${direction}`}>
      <h1 className="pb-display fs-3 mb-1">Where would you like to go?</h1>
      <p className="pb-muted mb-4">Choose a location to see what's available.</p>

      <div aria-live="polite">
        {branches.length === 0 ? (
          <p className="pb-muted">No locations are currently open for booking.</p>
        ) : (
          <div className="d-flex flex-column gap-2">
            {branches.map((branch) => {
              const isSelected = branch.branchId === selectedBranchId
              return (
                <button
                  key={branch.branchId}
                  type="button"
                  className={`pb-option d-flex align-items-center gap-3 ${isSelected ? 'is-selected' : ''}`.trim()}
                  onClick={() => onSelect(branch)}
                >
                  {isSelected && (
                    <span className="pb-option-check" aria-hidden="true">
                      ✓
                    </span>
                  )}
                  <span className="pb-option-icon" aria-hidden="true">
                    <Icon name="branches" size={20} />
                  </span>
                  <span>
                    <span className="d-block fw-semibold">{branch.branchName}</span>
                    <span className="d-block pb-muted small">
                      {branch.street}
                      {branch.barangay ? `, ${branch.barangay}` : ''}, {branch.city}
                    </span>
                  </span>
                </button>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}
