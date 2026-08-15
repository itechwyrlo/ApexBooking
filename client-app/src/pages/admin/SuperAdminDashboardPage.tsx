import { useState } from 'react'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { StatTile } from '../../components/common/StatTile'
import { TenantRequestTable } from '../../components/admin/TenantRequestTable'
import { TenantRequestDetailsModal } from '../../components/admin/TenantRequestDetailsModal'
import { useDashboardSummary } from '../../hooks/useDashboardSummary'
import { useTenantRequests } from '../../hooks/useTenantRequests'
import { useToast } from '../../hooks/useToast'
import { approveTenantRequest, rejectTenantRequest } from '../../services/superAdminService'
import type { ITenantRequest } from '../../interfaces/ITenantRequest'

export function SuperAdminDashboardPage() {
  const { summary } = useDashboardSummary()
  const { requests, isLoading, refetch } = useTenantRequests({ pageNumber: 1, pageSize: 5 })
  const { showToast } = useToast()
  const [selectedRequest, setSelectedRequest] = useState<ITenantRequest | null>(null)

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
      <PageHeader title="Platform Overview" description="Monitor tenant requests and platform activity." />

      {/* Tenant lifecycle group — every field in IPlatformDashboardSummary that describes a
          tenant/request's lifecycle state, grouped in one row instead of the previous arbitrary
          4-then-2 split. row-cols is used because 5 equal columns don't divide evenly into
          Bootstrap's 12-column col-* scheme. */}
      <div className="row g-3 mb-3 row-cols-2 row-cols-md-3 row-cols-lg-5">
        <div className="col">
          <StatTile label="Total Tenants" value={summary?.totalTenants ?? 0} icon="tenant-requests" tone="primary" />
        </div>
        <div className="col">
          <StatTile label="Active Tenants" value={summary?.activeTenants ?? 0} icon="check-circle" tone="success" />
        </div>
        <div className="col">
          <StatTile label="Pending Requests" value={summary?.pendingRequests ?? 0} icon="clock" tone="warning" />
        </div>
        <div className="col">
          <StatTile label="Approved Requests" value={summary?.approvedRequests ?? 0} icon="check-circle" tone="success" />
        </div>
        <div className="col">
          <StatTile label="Rejected Requests" value={summary?.rejectedRequests ?? 0} icon="x-circle" tone="danger" />
        </div>
      </div>

      {/* Booking activity group — a different kind of metric (platform booking volume, not tenant
          lifecycle), kept as its own row. */}
      <div className="row g-3 mb-3">
        <div className="col-6 col-lg-3">
          <StatTile label="Bookings Today" value={summary?.bookingsToday ?? 0} icon="appointments" tone="primary" />
        </div>
        <div className="col-6 col-lg-3">
          <StatTile label="Bookings This Month" value={summary?.bookingsThisMonth ?? 0} icon="calendar" tone="neutral" />
        </div>
      </div>

      <Card>
        <div className="d-flex align-items-center justify-content-between mb-3">
          <h2 className="fs-6 fw-semibold mb-0">Recent Tenant Requests</h2>
          <Button to="/admin/tenant-requests" variant="outline-secondary" size="sm">
            View All
          </Button>
        </div>
        <TenantRequestTable
          requests={requests}
          isLoading={isLoading}
          onViewDetails={setSelectedRequest}
          onApprove={handleApprove}
          onReject={handleReject}
        />
      </Card>

      <TenantRequestDetailsModal request={selectedRequest} onClose={() => setSelectedRequest(null)} />
    </div>
  )
}
