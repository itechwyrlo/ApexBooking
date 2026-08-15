import { useState } from 'react'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Pagination } from '../../components/common/Pagination'
import { TenantRequestTable } from '../../components/admin/TenantRequestTable'
import { TenantRequestDetailsModal } from '../../components/admin/TenantRequestDetailsModal'
import { useTenantRequests } from '../../hooks/useTenantRequests'
import { useToast } from '../../hooks/useToast'
import { approveTenantRequest, rejectTenantRequest } from '../../services/superAdminService'
import type { ITenantRequest } from '../../interfaces/ITenantRequest'

const PAGE_SIZE = 10

export function TenantRequestManagementPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { requests, total, isLoading, refetch } = useTenantRequests({ pageNumber, pageSize: PAGE_SIZE })
  const { showToast } = useToast()
  const [selectedRequest, setSelectedRequest] = useState<ITenantRequest | null>(null)

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const handleApprove = async (request: ITenantRequest) => {
    try {
      await approveTenantRequest(request.id)
      showToast('success', `${request.businessName} was approved.`)
      refetch()
    } catch {
      showToast('error', 'Failed to approve this request.')
    }
  }

  const handleReject = async (request: ITenantRequest) => {
    try {
      await rejectTenantRequest(request.id, 'Rejected from platform dashboard.')
      showToast('success', `${request.businessName} was rejected.`)
      refetch()
    } catch {
      showToast('error', 'Failed to reject this request.')
    }
  }

  return (
    <div>
      <PageHeader title="Tenant Requests" description="Review businesses requesting access to ApexBooking." />
      <Card>
        <TenantRequestTable
          requests={requests}
          isLoading={isLoading}
          onViewDetails={setSelectedRequest}
          onApprove={handleApprove}
          onReject={handleReject}
        />

        {!isLoading && total > 0 && (
          <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <p className="text-muted small mb-0">
              Page {pageNumber} of {totalPages} ({total} requests)
            </p>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
          </div>
        )}
      </Card>
      <TenantRequestDetailsModal request={selectedRequest} onClose={() => setSelectedRequest(null)} />
    </div>
  )
}
