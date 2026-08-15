import { useState } from 'react'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { Pagination } from '../../components/common/Pagination'
import { TeamMemberTable } from '../../components/team/TeamMemberTable'
import { AddTeamMemberModal } from '../../components/team/AddTeamMemberModal'
import { EditTeamMemberModal } from '../../components/team/EditTeamMemberModal'
import { RemoveTeamMemberModal } from '../../components/team/RemoveTeamMemberModal'
import { useTeamMembers } from '../../hooks/useTeamMembers'
import type { ITeamMember } from '../../interfaces/ITeamMember'

const PAGE_SIZE = 10

export function StaffPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { members, total, isLoading, error, refetch } = useTeamMembers({ pageNumber, pageSize: PAGE_SIZE })
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [editTarget, setEditTarget] = useState<ITeamMember | null>(null)
  const [removeTarget, setRemoveTarget] = useState<ITeamMember | null>(null)

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div>
      <PageHeader
        title="Team"
        description="Manage the team members who work at your business."
        primaryAction={<Button onClick={() => setIsAddModalOpen(true)}>Add Team Member</Button>}
      />
      <Card>
        {error && <p className="text-danger small mb-3">{error}</p>}

        <TeamMemberTable members={members} isLoading={isLoading} onEdit={setEditTarget} onRemove={setRemoveTarget} />

        {!isLoading && total > 0 && (
          <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <p className="text-muted small mb-0">
              Page {pageNumber} of {totalPages} ({total} team members)
            </p>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
          </div>
        )}
      </Card>
      <AddTeamMemberModal isOpen={isAddModalOpen} onClose={() => setIsAddModalOpen(false)} onCreated={refetch} />
      <EditTeamMemberModal isOpen={editTarget !== null} member={editTarget} onClose={() => setEditTarget(null)} onSaved={refetch} />
      <RemoveTeamMemberModal member={removeTarget} onClose={() => setRemoveTarget(null)} onRemoved={refetch} />
    </div>
  )
}
