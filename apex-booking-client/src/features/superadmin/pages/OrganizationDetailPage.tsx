import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { useOrganizationDetail } from '../hooks/useOrganizationDetail';
import AddTeamMemberModal from '../components/AddTeamMemberModal';

const statusBadge = (status: string) => {
  const classes: Record<string, string> = {
    Active: 'bg-success',
    Pending: 'bg-warning text-dark',
    Suspended: 'bg-danger',
    Deactivated: 'bg-secondary',
  };
  return <span className={`badge ${classes[status] ?? 'bg-secondary'}`}>{status}</span>;
};

const roleBadge = (role: string) => {
  const classes: Record<string, string> = {
    TenantAdmin: 'bg-primary',
    Manager: 'bg-info text-dark',
    Staff: 'bg-secondary',
    Customer: 'bg-light text-dark border',
  };
  return (
    <span className={`badge ${classes[role] ?? 'bg-secondary'}`}>
      {role === 'TenantAdmin' ? 'Admin' : role}
    </span>
  );
};

const StatCard: React.FC<{ label: string; value: number; icon: string; color?: string }> = ({
  label, value, icon, color = 'text-primary'
}) => (
  <div className="col">
    <div className="card border-0 shadow-sm h-100">
      <div className="card-body py-3 px-4">
        <div className="d-flex align-items-center gap-3">
          <i className={`fas ${icon} fs-5 ${color}`} />
          <div>
            <div className="fs-5 fw-bold">{value}</div>
            <div className="text-muted" style={{ fontSize: 12 }}>{label}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
);

const OrganizationDetailPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const { loadDetail, createUser, assignUser, resendInvite, detail, isLoading, actionLoading, error, clearError } =
    useOrganizationDetail(slug!);
  const [showAddModal, setShowAddModal] = useState(false);
  const [resendSuccess, setResendSuccess] = useState<string | null>(null);

  const handleResendInvite = async (userId: string, userName: string) => {
    const ok = await resendInvite(userId);
    if (ok) setResendSuccess(`Invitation resent to ${userName}.`);
  };

  useEffect(() => {
    loadDetail();
  }, [loadDetail]);

  if (isLoading) {
    return (
      <div className="container-fluid">
        <div className="text-muted small p-4">Loading organization...</div>
      </div>
    );
  }

  if (!detail) {
    return (
      <div className="container-fluid">
        {error && (
          <Alert variant="error" dismissible onDismiss={clearError} className="mb-4">
            {error}
          </Alert>
        )}
        <div className="text-muted small">Organization not found.</div>
      </div>
    );
  }

  return (
    <div className="container-fluid">
      {/* Header */}
      <div className="mb-4 d-flex align-items-start justify-content-between">
        <div className="d-flex align-items-center gap-3">
          <button
            className="btn btn-link p-0 text-muted"
            onClick={() => navigate('/superadmin/organizations')}
          >
            <i className="fas fa-arrow-left" />
          </button>
          <div>
            <div className="d-flex align-items-center gap-2">
              <h4 className="fw-bold mb-0">{detail.businessName}</h4>
              {statusBadge(detail.status)}
            </div>
            <div className="text-muted small mt-1">
              /book/{detail.slug}
              <a
                href={`/book/${detail.slug}`}
                target="_blank"
                rel="noreferrer"
                className="ms-2 text-primary small"
              >
                <i className="fas fa-external-link-alt" />
              </a>
            </div>
          </div>
        </div>
      </div>

      {error && (
        <Alert variant="error" dismissible onDismiss={clearError} className="mb-4">
          {error}
        </Alert>
      )}
      {resendSuccess && (
        <Alert variant="success" dismissible onDismiss={() => setResendSuccess(null)} className="mb-4">
          {resendSuccess}
        </Alert>
      )}

      {/* Stats */}
      <div className="row row-cols-2 row-cols-md-5 g-3 mb-4">
        <StatCard label="Bookings" value={detail.bookingCount} icon="fa-calendar-check" color="text-primary" />
        <StatCard label="Services" value={detail.serviceCount} icon="fa-concierge-bell" color="text-info" />
        <StatCard label="Staff" value={detail.staffCount} icon="fa-user-tie" color="text-success" />
        <StatCard label="Clients" value={detail.clientCount} icon="fa-users" color="text-warning" />
        <StatCard label="Users" value={detail.userCount} icon="fa-id-badge" color="text-secondary" />
      </div>

      {/* Team Members */}
      <div className="card border-0 shadow-sm">
        <div className="card-header bg-white border-bottom d-flex align-items-center justify-content-between py-3">
          <span className="fw-semibold">Team Members</span>
          <button
            className="btn btn-success btn-sm d-flex align-items-center gap-1"
            onClick={() => setShowAddModal(true)}
          >
            <i className="fas fa-plus" />
            Add User
          </button>
        </div>
        <div className="card-body p-0">
          {!detail.users.length ? (
            <div className="p-4 text-muted small">No users in this organization.</div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead className="table-light">
                  <tr>
                    <th className="ps-4 small fw-semibold text-muted">Member</th>
                    <th className="small fw-semibold text-muted">Role</th>
                    <th className="small fw-semibold text-muted" colSpan={2}>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.users.map(user => (
                    <tr key={user.id}>
                      <td className="ps-4">
                        <div className="d-flex align-items-center gap-3">
                          <div className="rounded-circle bg-secondary d-flex align-items-center justify-content-center flex-shrink-0 text-white fw-bold sa-team-avatar">
                            {user.fullName.charAt(0).toUpperCase()}
                          </div>
                          <div>
                            <div className="small fw-semibold">{user.fullName}</div>
                            <div className="text-muted small">{user.email}</div>
                          </div>
                        </div>
                      </td>
                      <td>{roleBadge(user.role)}</td>
                      <td>
                        <span
                          className={`badge ${user.status === 'Active' ? 'bg-success' : user.status === 'Invited' ? 'bg-warning text-dark' : 'bg-secondary'}`}
                        >
                          {user.status}
                        </span>
                      </td>
                      <td>
                        {user.status === 'Invited' && (
                          <Button
                            variant="secondary"
                            onClick={() => handleResendInvite(user.id, user.fullName)}
                            loading={actionLoading}
                            disabled={actionLoading}
                            className="btn btn-outline-secondary btn-sm"
                          >
                            <i className="fas fa-paper-plane me-1" />
                            Resend Invite
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <AddTeamMemberModal
        show={showAddModal}
        onClose={() => setShowAddModal(false)}
        onCreateUser={createUser}
        onAssignUser={assignUser}
        isLoading={actionLoading}
        error={error}
        clearError={clearError}
      />
    </div>
  );
};

export default OrganizationDetailPage;
