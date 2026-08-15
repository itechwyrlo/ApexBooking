import { Modal } from '../common/Modal'
import { RequestStatusBadge } from './RequestStatusBadge'
import type { ITenantRequest } from '../../interfaces/ITenantRequest'

interface ITenantRequestDetailsModalProps {
  request: ITenantRequest | null
  onClose: () => void
}

export function TenantRequestDetailsModal({ request, onClose }: ITenantRequestDetailsModalProps) {
  return (
    <Modal isOpen={!!request} title="Request Details" onClose={onClose}>
      {request && (
        <dl className="row mb-0">
          <dt className="col-5 text-muted fw-normal">Business Name</dt>
          <dd className="col-7">{request.businessName}</dd>

          <dt className="col-5 text-muted fw-normal">Business Type</dt>
          <dd className="col-7">{request.businessType}</dd>

          <dt className="col-5 text-muted fw-normal">Requested Slug</dt>
          <dd className="col-7">{request.requestedSlug}</dd>

          <dt className="col-5 text-muted fw-normal">Requested Plan</dt>
          <dd className="col-7">{request.requestedPlan}</dd>

          <dt className="col-5 text-muted fw-normal">Owner Name</dt>
          <dd className="col-7">{request.ownerFirstName} {request.ownerLastName}</dd>

          <dt className="col-5 text-muted fw-normal">Owner Email</dt>
          <dd className="col-7">{request.ownerEmail}</dd>

          <dt className="col-5 text-muted fw-normal">Requested Date</dt>
          <dd className="col-7">{new Date(request.requestedAt).toLocaleDateString()}</dd>

          <dt className="col-5 text-muted fw-normal">Request Status</dt>
          <dd className="col-7 mb-0">
            <RequestStatusBadge status={request.status} />
          </dd>
        </dl>
      )}
    </Modal>
  )
}
