import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSuperAdminOrganizations } from '../hooks/useSuperAdminOrganizations';
import { useTenantRequests } from '../hooks/useTenantRequests';
import { Alert } from '../../../components/ui/Alert';

const statusBadge = (status: string) => {
  const classes: Record<string, string> = {
    Active: 'bg-success',
    Trial: 'bg-info',
    Pending: 'bg-warning text-dark',
    Suspended: 'bg-danger',
    Deactivated: 'bg-secondary',
  };
  return (
    <span className={`badge ${classes[status] ?? 'bg-secondary'}`}>
      {status}
    </span>
  );
};

const orgInitials = (name: string) =>
  name
    .split(' ')
    .slice(0, 2)
    .map(w => w[0])
    .join('')
    .toUpperCase();

const AVATAR_COLORS = [
  '#6366f1', '#8b5cf6', '#ec4899', '#14b8a6',
  '#f59e0b', '#ef4444', '#3b82f6', '#10b981',
];
const avatarColor = (slug: string) =>
  AVATAR_COLORS[slug.charCodeAt(0) % AVATAR_COLORS.length];

const SuperAdminOverviewPage: React.FC = () => {
  const navigate = useNavigate();
  const { loadOverview, overview, isLoading, error, clearError } = useSuperAdminOrganizations();
  const { fetchRequests, requests: pendingRequests, isLoading: isPendingLoading } = useTenantRequests();

  useEffect(() => {
    loadOverview();
    fetchRequests('Pending');
  }, [loadOverview, fetchRequests]);

  return (
    <div className="container-fluid">
      <div className="d-flex align-items-center justify-content-between mb-4">
        <div>
          <h4 className="fw-bold mb-1">Platform Overview</h4>
          <div className="text-muted small">Manage all organizations on the platform.</div>
        </div>
        <div className="d-flex gap-2">
          <button
            className="btn btn-outline-primary btn-sm d-flex align-items-center gap-1"
            onClick={() => navigate('/superadmin/requests')}
          >
            <i className="fas fa-inbox" />
            View Requests
            {pendingRequests.length > 0 && (
              <span className="badge bg-warning text-dark ms-1">{pendingRequests.length}</span>
            )}
          </button>
          <button
            className="btn btn-success btn-sm d-flex align-items-center gap-1"
            onClick={() => navigate('/superadmin/organizations/new')}
          >
            <i className="fas fa-plus" />
            New Organization
          </button>
        </div>
      </div>

      {error && (
        <Alert variant="error" dismissible onDismiss={clearError} className="mb-4">
          {error}
        </Alert>
      )}

      {/* Stats cards */}
      <div className="row g-3 mb-4">
        <div className="col-sm-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <div className="text-muted small mb-1">Total Organizations</div>
              <div className="fs-3 fw-bold">
                {isLoading ? '—' : (overview?.totalOrgs ?? 0)}
              </div>
            </div>
          </div>
        </div>
        <div className="col-sm-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <div className="text-muted small mb-1">Active</div>
              <div className="fs-3 fw-bold text-success">
                {isLoading ? '—' : (overview?.activeOrgs ?? 0)}
              </div>
            </div>
          </div>
        </div>
        <div className="col-sm-3">
          <div className="card border-0 shadow-sm h-100">
            <div className="card-body">
              <div className="text-muted small mb-1">Inactive</div>
              <div className="fs-3 fw-bold text-danger">
                {isLoading ? '—' : (overview?.inactiveOrgs ?? 0)}
              </div>
            </div>
          </div>
        </div>
        <div className="col-sm-3">
          <div className="card border-0 shadow-sm h-100 sa-clickable" onClick={() => navigate('/superadmin/requests')}>
            <div className="card-body">
              <div className="text-muted small mb-1">Pending Requests</div>
              <div className="fs-3 fw-bold text-warning">
                {isPendingLoading ? '—' : pendingRequests.length}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Organizations table */}
      <div className="card border-0 shadow-sm">
        <div className="card-header bg-white border-bottom d-flex align-items-center justify-content-between py-3">
          <span className="fw-semibold">All Organizations</span>
        </div>
        <div className="card-body p-0">
          {isLoading ? (
            <div className="p-4 text-muted small">Loading organizations...</div>
          ) : !overview?.organizations?.length ? (
            <div className="p-4 text-muted small">No organizations found.</div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead className="table-light">
                  <tr>
                    <th className="ps-4 small fw-semibold text-muted">Organization</th>
                    <th className="small fw-semibold text-muted">Booking URL</th>
                    <th className="small fw-semibold text-muted">Users</th>
                    <th className="small fw-semibold text-muted">Status</th>
                    <th className="small fw-semibold text-muted">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {overview.organizations.map(org => (
                    <tr key={org.id}>
                      <td className="ps-4">
                        <div className="d-flex align-items-center gap-3">
                          <div
                            className="rounded d-flex align-items-center justify-content-center flex-shrink-0 sa-org-avatar"
                            style={{ backgroundColor: avatarColor(org.slug) }}
                          >
                            <span className="text-white fw-bold small">
                              {orgInitials(org.businessName)}
                            </span>
                          </div>
                          <div>
                            <div className="fw-semibold small">{org.businessName}</div>
                            <div className="text-muted small">{org.ownerEmail}</div>
                          </div>
                        </div>
                      </td>
                      <td>
                        <span className="text-muted small">/book/{org.slug}</span>
                      </td>
                      <td>
                        <span className="small">{org.userCount}</span>
                      </td>
                      <td>{statusBadge(org.status)}</td>
                      <td>
                        <button
                          className="btn btn-outline-primary btn-sm"
                          onClick={() => navigate(`/superadmin/organizations/${org.slug}`)}
                        >
                          Manage
                        </button>
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
  );
};

export default SuperAdminOverviewPage;
