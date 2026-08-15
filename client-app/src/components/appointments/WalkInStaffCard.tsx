import { Badge, type BadgeTone } from '../common/Badge'
import type { IWalkInStaffAvailability } from '../../interfaces/IWalkInStaffAvailability'

const UNAVAILABLE_TONE: Record<string, BadgeTone> = {
  'Off Today': 'neutral',
  'On Time Off': 'neutral',
  'Fully Booked': 'warning',
  'Branch Closed': 'neutral',
}

interface IWalkInStaffCardProps {
  staff: IWalkInStaffAvailability
  isRecommended: boolean
  isSelected: boolean
  selectedTime: string | null
  onSelectStaff: () => void
  onSelectTime: (rawTime: string) => void
}

export function WalkInStaffCard({ staff, isRecommended, isSelected, selectedTime, onSelectStaff, onSelectTime }: IWalkInStaffCardProps) {
  const isAvailable = staff.recommendedTimeRaw !== null

  return (
    <div className={`walkin-staff-card p-3 border rounded-3 mb-2 ${isSelected ? 'border-primary' : ''} ${!isAvailable ? 'bg-body-secondary' : ''}`}>
      <button
        type="button"
        className="btn btn-link p-0 text-decoration-none text-reset w-100 text-start d-flex justify-content-between align-items-start"
        disabled={!isAvailable}
        onClick={onSelectStaff}
      >
        <span>
          <span className="fw-semibold d-block">{staff.fullName}</span>
          {staff.customJobTitle && <span className="text-muted small">{staff.customJobTitle}</span>}
        </span>
        <span className="d-flex flex-column align-items-end gap-1">
          {isRecommended && isAvailable && <Badge tone="primary">Recommended</Badge>}
          {!isAvailable && <Badge tone={UNAVAILABLE_TONE[staff.unavailableReason ?? ''] ?? 'neutral'}>{staff.unavailableReason}</Badge>}
        </span>
      </button>

      {isAvailable && (
        <div className="mt-2 small">
          <span className="text-body">
            {staff.isAvailableNow ? 'Available now' : `Next available ${staff.recommendedTimeDisplay}`}
            {staff.availableUntilDisplay && ` · Available until ${staff.availableUntilDisplay}`}
          </span>
        </div>
      )}

      {isSelected && (staff.recommendedTimeDisplay || staff.alternateTimes.length > 0) && (
        <div className="mt-2 d-flex flex-wrap gap-2">
          {staff.recommendedTimeRaw && (
            <button
              type="button"
              className={`btn btn-sm ${selectedTime === staff.recommendedTimeRaw ? 'btn-primary' : 'btn-outline-secondary'}`}
              onClick={() => onSelectTime(staff.recommendedTimeRaw!)}
            >
              {staff.recommendedTimeDisplay}
            </button>
          )}
          {staff.alternateTimes.map((option) => (
            <button
              key={option.raw}
              type="button"
              className={`btn btn-sm ${selectedTime === option.raw ? 'btn-primary' : 'btn-outline-secondary'}`}
              onClick={() => onSelectTime(option.raw)}
            >
              {option.display}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
