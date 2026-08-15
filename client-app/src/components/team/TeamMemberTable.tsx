import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RowActions, type IRowAction } from '../common/RowActions'
import { TeamRoleBadge } from './TeamRoleBadge'
import { TeamStatusBadge } from './TeamStatusBadge'
import { Role } from '../../types/Role'
import { TenantMemberStatus } from '../../types/TenantMemberStatus'
import type { ITeamMember } from '../../interfaces/ITeamMember'

const COLUMNS = ['Team Member', 'Role', 'Status', 'Contact Number', 'Job Title', '']

interface ITeamMemberTableProps {
  members: ITeamMember[]
  isLoading?: boolean
  onEdit: (member: ITeamMember) => void
  onRemove: (member: ITeamMember) => void
}

export function TeamMemberTable({ members, isLoading, onEdit, onRemove }: ITeamMemberTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (members.length === 0) {
    return (
      <EmptyState
        icon="staff"
        title="No team members yet"
        description="Add your team to start assigning appointments and tracking availability."
      />
    )
  }

  return (
    <div className="table-responsive">
      <table className="table table-stack align-middle mb-0">
        <thead>
          <tr className="text-muted small text-uppercase">
            {COLUMNS.map((column) => (
              <th key={column} scope="col" className="fw-semibold">
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {members.map((member) => {
            const isDeactivated = member.status === TenantMemberStatus.Deactivated
            const isOwner = member.role === Role.Owner

            const actions: IRowAction[] = [{ label: `Edit ${member.fullName}`, icon: 'edit', tone: 'edit', onClick: () => onEdit(member) }]
            if (!isOwner && !isDeactivated) {
              actions.push({ label: `Remove ${member.fullName}`, icon: 'trash', tone: 'delete', onClick: () => onRemove(member) })
            }

            return (
              <tr key={member.id} className={isDeactivated ? 'opacity-50' : ''}>
                <td data-label="Team Member">
                  <div className="fw-semibold">{member.fullName}</div>
                  <div className="text-muted small">{member.email}</div>
                </td>
                <td data-label="Role">
                  <TeamRoleBadge role={member.role} />
                </td>
                <td data-label="Status">
                  <TeamStatusBadge status={member.status} />
                </td>
                <td data-label="Contact Number">{member.contactNumber || '—'}</td>
                <td data-label="Job Title">{member.customJobTitle || '—'}</td>
                <td className="text-end" data-label="Actions">
                  <RowActions actions={actions} />
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
