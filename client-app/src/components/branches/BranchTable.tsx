import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RowActions } from '../common/RowActions'
import { ActiveStatusBadge } from '../common/ActiveStatusBadge'
import type { IBranch } from '../../interfaces/IBranch'

const COLUMNS = ['Branch', 'Address', 'Time Zone', 'Status', '']

interface IBranchTableProps {
  branches: IBranch[]
  isLoading?: boolean
  onEdit: (branchId: string) => void
}

export function BranchTable({ branches, isLoading, onEdit }: IBranchTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={3} />
  }

  if (branches.length === 0) {
    return (
      <EmptyState
        icon="branches"
        title="No branches yet"
        description="Add your first location so customers know where to find you."
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
          {branches.map((branch) => (
            <tr key={branch.id}>
              <td className="fw-semibold" data-label="Branch">
                {branch.name}
              </td>
              <td data-label="Address">
                {branch.street}
                {branch.barangay ? `, ${branch.barangay}` : ''}, {branch.city}, {branch.province} {branch.zipCode}
              </td>
              <td data-label="Time Zone">{branch.timeZoneId}</td>
              <td data-label="Status">
                <ActiveStatusBadge isActive={branch.isActive} />
              </td>
              <td className="text-end" data-label="Actions">
                <RowActions actions={[{ label: `Edit ${branch.name}`, icon: 'edit', tone: 'edit', onClick: () => onEdit(branch.id) }]} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
