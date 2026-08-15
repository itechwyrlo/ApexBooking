import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RowActions, type IRowAction } from '../common/RowActions'
import { RequestStatusBadge } from './RequestStatusBadge'
import { TenantRequestStatus } from '../../types/TenantRequestStatus'
import type { ITenantRequest } from '../../interfaces/ITenantRequest'

const COLUMNS = ['Business Name', 'Business Type', 'Owner', 'Owner Email', 'Requested Plan', 'Requested Date', 'Status', 'Actions']

interface ITenantRequestTableProps {
  requests: ITenantRequest[]
  isLoading?: boolean
  onViewDetails: (request: ITenantRequest) => void
  onApprove: (request: ITenantRequest) => void
  onReject: (request: ITenantRequest) => void
}

export function TenantRequestTable({ requests, isLoading, onViewDetails, onApprove, onReject }: ITenantRequestTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (requests.length === 0) {
    return (
      <EmptyState
        icon="tenant-requests"
        title="No tenant requests available."
        description="New business requests to join ApexBooking will appear here."
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
          {requests.map((request) => {
            const actions: IRowAction[] = [
              { label: `View details for ${request.businessName}`, icon: 'eye', tone: 'view', onClick: () => onViewDetails(request) },
            ]
            if (request.status === TenantRequestStatus.Pending) {
              actions.push(
                { label: `Approve ${request.businessName}`, icon: 'check-circle', tone: 'primary', onClick: () => onApprove(request) },
                { label: `Reject ${request.businessName}`, icon: 'x-circle', tone: 'delete', onClick: () => onReject(request) },
              )
            }

            return (
              <tr key={request.id}>
                <td className="fw-semibold" data-label="Business Name">
                  {request.businessName}
                </td>
                <td data-label="Business Type">{request.businessType}</td>
                <td data-label="Owner">
                  {request.ownerFirstName} {request.ownerLastName}
                </td>
                <td data-label="Owner Email">{request.ownerEmail}</td>
                <td data-label="Requested Plan">{request.requestedPlan}</td>
                <td data-label="Requested Date">{new Date(request.requestedAt).toLocaleDateString()}</td>
                <td data-label="Status">
                  <RequestStatusBadge status={request.status} />
                </td>
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
