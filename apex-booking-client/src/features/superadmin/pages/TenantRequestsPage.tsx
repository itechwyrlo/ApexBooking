import React, { useEffect, useState } from 'react';
import { useTenantRequests } from '../hooks/useTenantRequests';
import { Alert } from '../../../components/ui/Alert';
import type { TenantRequestDto, TenantRequestStatus } from '../types';

const STATUS_TABS: { label: string; value: string | undefined }[] = [
  { label: 'All', value: undefined },
  { label: 'Pending', value: 'Pending' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Rejected', value: 'Rejected' },
];

const statusBadge = (status: TenantRequestStatus) => {
  const classes: Record<TenantRequestStatus, string> = {
    Pending: 'bg-warning text-dark',
    Approved: 'bg-success',
    Rejected: 'bg-danger',
  };
  return <span className={`badge ${classes[status]}`}>{status}</span>;
};

const TenantRequestsPage: React.FC = () => {
  const {
    requests,
    selectedRequest,
    isLoading,
    isSubmitting,
    error,
    actionError,
    clearError,
    clearActionError,
    fetchRequests,
    fetchById,
    approve,
    reject,
  } = useTenantRequests();

  const [activeTab, setActiveTab] = useState<string | undefined>(undefined);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Approve form state
  const [slug, setSlug] = useState('');
  const [trialDays, setTrialDays] = useState(14);
  const [approveSuccess, setApproveSuccess] = useState(false);

  // Reject form state
  const [reason, setReason] = useState('');
  const [rejectSuccess, setRejectSuccess] = useState(false);

  useEffect(() => {
    fetchRequests(activeTab);
  }, [activeTab, fetchRequests]);

  const handleSelectRow = (req: TenantRequestDto) => {
    setSelectedId(req.id);
    setSlug('');
    setTrialDays(14);
    setReason('');
    setApproveSuccess(false);
    setRejectSuccess(false);
    clearActionError();
    fetchById(req.id);
  };

  const handleApprove = async () => {
    if (!selectedId || !slug.trim()) return;
    const ok = await approve(selectedId, { slug: slug.trim(), trialDays });
    if (ok) {
      setApproveSuccess(true);
      fetchRequests(activeTab);
    }
  };

  const handleReject = async () => {
    if (!selectedId || !reason.trim()) return;
    const ok = await reject(selectedId, { reason: reason.trim() });
    if (ok) {
      setRejectSuccess(true);
      fetchRequests(activeTab);
    }
  };

  return (
    <div className="container-fluid">
      <div className="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h4 className="fw-bold mb-1">Access Requests</h4>
          <div className="text-muted small">Review and approve incoming tenant registration requests.</div>
        </div>
      </div>

      {error && (
        <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
          {error}
        </Alert>
      )}

      <div className="row g-3">
        {/* Left: request list */}
        <div className={selectedId ? 'col-lg-6' : 'col-12'}>
          {/* Status filter tabs */}
          <ul className="nav nav-tabs mb-3">
            {STATUS_TABS.map(tab => (
              <li className="nav-item" key={tab.label}>
                <button
                  className={`nav-link${activeTab === tab.value ? ' active' : ''}`}
                  onClick={() => {
                    setActiveTab(tab.value);
                    setSelectedId(null);
                  }}
                >
                  {tab.label}
                </button>
              </li>
            ))}
          </ul>

          <div className="card border-0 shadow-sm">
            <div className="card-body p-0">
              {isLoading ? (
                <div className="p-4 text-muted small">Loading requests...</div>
              ) : !requests.length ? (
                <div className="p-4 text-muted small">No requests found.</div>
              ) : (
                <div className="table-responsive">
                  <table className="table table-hover align-middle mb-0">
                    <thead className="table-light">
                      <tr>
                        <th className="ps-4 small fw-semibold text-muted">Business</th>
                        <th className="small fw-semibold text-muted">Plan</th>
                        <th className="small fw-semibold text-muted">Status</th>
                        <th className="small fw-semibold text-muted">Submitted</th>
                      </tr>
                    </thead>
                    <tbody>
                      {requests.map(req => (
                        <tr
                          key={req.id}
                          onClick={() => handleSelectRow(req)}
                          style={{ cursor: 'pointer' }}
                          className={selectedId === req.id ? 'table-active' : ''}
                        >
                          <td className="ps-4">
                            <div className="fw-semibold small">{req.businessName}</div>
                            <div className="text-muted" style={{ fontSize: 12 }}>{req.ownerEmail}</div>
                          </td>
                          <td>
                            <span className="small">{req.plan}</span>
                          </td>
                          <td>{statusBadge(req.status)}</td>
                          <td>
                            <span className="small text-muted">
                              {new Date(req.createdAt).toLocaleDateString()}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Right: detail panel */}
        {selectedId && (
          <div className="col-lg-6">
            <div className="card border-0 shadow-sm h-100">
              {!selectedRequest || selectedRequest.id !== selectedId ? (
                <div className="card-body d-flex align-items-center justify-content-center text-muted small">
                  Loading...
                </div>
              ) : (
                <div className="card-body">
                  <div className="d-flex align-items-start justify-content-between mb-3">
                    <div>
                      <h6 className="fw-bold mb-1">{selectedRequest.businessName}</h6>
                      <div className="text-muted small">{selectedRequest.ownerFullName} · {selectedRequest.ownerEmail}</div>
                      {selectedRequest.ownerPhone && (
                        <div className="text-muted small">{selectedRequest.ownerPhone}</div>
                      )}
                    </div>
                    <div className="d-flex flex-column align-items-end gap-1">
                      {statusBadge(selectedRequest.status)}
                      <span className="badge bg-light text-dark border">{selectedRequest.plan}</span>
                    </div>
                  </div>

                  {selectedRequest.message && (
                    <div className="mb-3 p-3 rounded" style={{ background: '#f8fafc', fontSize: 13 }}>
                      <div className="text-muted small fw-semibold mb-1">Message</div>
                      {selectedRequest.message}
                    </div>
                  )}

                  {selectedRequest.status === 'Rejected' && selectedRequest.rejectionReason && (
                    <div className="mb-3 p-3 rounded bg-danger bg-opacity-10" style={{ fontSize: 13 }}>
                      <div className="text-danger small fw-semibold mb-1">Rejection Reason</div>
                      {selectedRequest.rejectionReason}
                    </div>
                  )}

                  {actionError && (
                    <Alert variant="error" dismissible onDismiss={clearActionError} className="mb-3">
                      {actionError}
                    </Alert>
                  )}

                  {selectedRequest.status === 'Pending' && (
                    <>
                      {/* Approve */}
                      {!approveSuccess && !rejectSuccess && (
                        <div className="mb-3 p-3 rounded border">
                          <div className="fw-semibold small mb-2">Approve</div>
                          <div className="mb-2">
                            <label className="form-label small mb-1">Slug</label>
                            <input
                              type="text"
                              className="form-control form-control-sm"
                              placeholder="e.g. lumiere-salon"
                              value={slug}
                              onChange={e => setSlug(e.target.value)}
                            />
                          </div>
                          <div className="mb-3">
                            <label className="form-label small mb-1">Trial days</label>
                            <input
                              type="number"
                              className="form-control form-control-sm"
                              min={1}
                              max={90}
                              value={trialDays}
                              onChange={e => setTrialDays(Number(e.target.value))}
                            />
                          </div>
                          <button
                            className="btn btn-success btn-sm w-100"
                            onClick={handleApprove}
                            disabled={isSubmitting || !slug.trim()}
                          >
                            {isSubmitting ? 'Approving...' : 'Approve Request'}
                          </button>
                        </div>
                      )}

                      {/* Reject */}
                      {!approveSuccess && !rejectSuccess && (
                        <div className="p-3 rounded border">
                          <div className="fw-semibold small mb-2">Reject</div>
                          <div className="mb-3">
                            <label className="form-label small mb-1">Reason</label>
                            <textarea
                              className="form-control form-control-sm"
                              rows={3}
                              placeholder="Reason for rejection..."
                              value={reason}
                              onChange={e => setReason(e.target.value)}
                            />
                          </div>
                          <button
                            className="btn btn-outline-danger btn-sm w-100"
                            onClick={handleReject}
                            disabled={isSubmitting || !reason.trim()}
                          >
                            {isSubmitting ? 'Rejecting...' : 'Reject Request'}
                          </button>
                        </div>
                      )}

                      {approveSuccess && (
                        <div className="alert alert-success small mb-0">
                          Request approved. Setup email sent to {selectedRequest.ownerEmail}.
                        </div>
                      )}

                      {rejectSuccess && (
                        <div className="alert alert-secondary small mb-0">
                          Request rejected and applicant notified.
                        </div>
                      )}
                    </>
                  )}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default TenantRequestsPage;
