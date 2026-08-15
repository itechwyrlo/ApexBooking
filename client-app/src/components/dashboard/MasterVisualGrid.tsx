import { EmptyState } from '../common/EmptyState'
import { StaffLineupTimeline } from './StaffLineupTimeline'
import type { ITeamMember } from '../../interfaces/ITeamMember'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface IMasterVisualGridProps {
  staff: ITeamMember[]
  bookings: ITenantBooking[]
  isLoading: boolean
}

export function MasterVisualGrid({ staff, bookings, isLoading }: IMasterVisualGridProps) {
  if (!isLoading && staff.length === 0) {
    return (
      <EmptyState
        icon="dashboard"
        title="No active staff yet"
        description="Add team members and mark them active to see their schedules here."
      />
    )
  }

  return (
    <div className="d-flex gap-3 overflow-auto pb-2">
      {staff.map((member) => (
        <div key={member.id} style={{ minWidth: 220, flexShrink: 0 }}>
          <div className="d-flex align-items-center gap-2 mb-2">
            {member.photoUrl && <img src={member.photoUrl} width={28} height={28} className="rounded-circle" alt="" />}
            <span className="fw-semibold small">{member.fullName}</span>
          </div>
          <StaffLineupTimeline bookings={bookings.filter((b) => b.staffId === member.id)} isLoading={isLoading} />
        </div>
      ))}
    </div>
  )
}
