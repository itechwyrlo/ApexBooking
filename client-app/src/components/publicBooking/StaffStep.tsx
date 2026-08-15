import type { IPublicStaff } from '../../interfaces/publicBooking/IPublicStaff'
import type { WizardDirection } from '../../hooks/usePublicBookingWizard'

interface IStaffStepProps {
  staff: IPublicStaff[]
  isLoading: boolean
  selectedStaffId: string | null
  direction: WizardDirection
  onSelect: (staffMember: IPublicStaff) => void
}

export function StaffStep({ staff, isLoading, selectedStaffId, direction, onSelect }: IStaffStepProps) {
  return (
    <div className={`pb-step-enter-${direction}`}>
      <h1 className="pb-display fs-3 mb-1">Who would you like to see?</h1>
      <p className="pb-muted mb-4">Choose a team member for this service.</p>

      <div aria-live="polite">
        {isLoading ? (
          <p className="pb-muted">Loading team members...</p>
        ) : staff.length === 0 ? (
          <p className="pb-muted">No one is currently available for this service.</p>
        ) : (
          <div className="d-flex flex-column gap-2">
            {staff.map((member) => {
              const isSelected = member.tenantMemberId === selectedStaffId
              return (
                <button
                  key={member.tenantMemberId}
                  type="button"
                  className={`pb-option d-flex align-items-center gap-3 ${isSelected ? 'is-selected' : ''}`.trim()}
                  onClick={() => onSelect(member)}
                >
                  {isSelected && (
                    <span className="pb-option-check" aria-hidden="true">
                      ✓
                    </span>
                  )}
                  <span
                    className="d-inline-flex align-items-center justify-content-center rounded-circle flex-shrink-0 overflow-hidden"
                    style={{ width: 40, height: 40, backgroundColor: 'var(--pb-accent-soft)' }}
                    aria-hidden="true"
                  >
                    {member.photoUrl ? (
                      <img src={member.photoUrl} alt="" width={40} height={40} style={{ objectFit: 'cover' }} />
                    ) : (
                      <span className="pb-display fw-semibold" style={{ color: 'var(--pb-accent)' }}>
                        {member.fullName.charAt(0).toUpperCase()}
                      </span>
                    )}
                  </span>
                  <span>
                    <span className="d-block fw-semibold">{member.fullName}</span>
                    {member.customJobTitle && <span className="d-block pb-muted small">{member.customJobTitle}</span>}
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
