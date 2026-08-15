import { useState } from 'react'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { BranchTable } from '../../components/branches/BranchTable'
import { BranchModal } from '../../components/branches/BranchModal'
import { useBranches } from '../../hooks/useBranches'

export function BranchesPage() {
  const { branches, isLoading, error, refetch } = useBranches()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingBranchId, setEditingBranchId] = useState<string | null>(null)

  const openAddModal = () => {
    setEditingBranchId(null)
    setIsModalOpen(true)
  }

  const openEditModal = (branchId: string) => {
    setEditingBranchId(branchId)
    setIsModalOpen(true)
  }

  return (
    <div>
      <PageHeader
        title="Branches"
        description="Manage the locations where customers can book with you."
        primaryAction={<Button onClick={openAddModal}>Add Branch</Button>}
      />
      <Card>
        {error && <p className="text-danger small mb-3">{error}</p>}

        <BranchTable branches={branches} isLoading={isLoading} onEdit={openEditModal} />
      </Card>
      <BranchModal
        isOpen={isModalOpen}
        branchId={editingBranchId}
        onClose={() => setIsModalOpen(false)}
        onSaved={refetch}
      />
    </div>
  )
}
