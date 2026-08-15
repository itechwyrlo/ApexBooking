import { useState } from 'react'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { Pagination } from '../../components/common/Pagination'
import { ServiceTable } from '../../components/services/ServiceTable'
import { AddServiceModal } from '../../components/services/AddServiceModal'
import { useServices } from '../../hooks/useServices'
import type { IService } from '../../interfaces/IService'

const PAGE_SIZE = 10

export function ServicesPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { services, total, isLoading, error, refetch } = useServices({ pageNumber, pageSize: PAGE_SIZE })
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingService, setEditingService] = useState<IService | null>(null)

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const openAddModal = () => {
    setEditingService(null)
    setIsModalOpen(true)
  }

  const openEditModal = (service: IService) => {
    setEditingService(service)
    setIsModalOpen(true)
  }

  return (
    <div>
      <PageHeader
        title="Services"
        description="Add the services customers can book, like haircuts, checkups, or repairs."
        primaryAction={
          <div className="d-grid gap-2 d-sm-flex">
            <Button icon="plus" onClick={openAddModal}>
              Add Service
            </Button>
          </div>
        }
      />
      <Card>
        {error && <p className="text-danger small mb-3">{error}</p>}

        <div key={isLoading ? 'loading' : `page-${pageNumber}`} className="list-fade-in">
          <ServiceTable services={services} isLoading={isLoading} onEdit={openEditModal} />

          {!isLoading && total > 0 && (
            <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
              <p className="text-muted small mb-0">
                Page {pageNumber} of {totalPages} ({total} services)
              </p>
              <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
            </div>
          )}
        </div>
      </Card>
      <AddServiceModal
        isOpen={isModalOpen}
        service={editingService}
        onClose={() => setIsModalOpen(false)}
        onSaved={refetch}
      />
    </div>
  )
}
