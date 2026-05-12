import React, { useState } from 'react';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import type { CreateTenantUserRequest, AssignExistingUserRequest, TenantUserDto } from '../types';

type Tab = 'create' | 'assign';

const ROLE_OPTIONS = ['TenantAdmin', 'Manager', 'Staff', 'Customer'];

interface Props {
  show: boolean;
  onClose: () => void;
  onCreateUser: (req: CreateTenantUserRequest) => Promise<TenantUserDto | null>;
  onAssignUser: (req: AssignExistingUserRequest) => Promise<TenantUserDto | null>;
  isLoading: boolean;
  error: string | null;
  clearError: () => void;
}

const EMPTY_CREATE: CreateTenantUserRequest = {
  fullName: '',
  email: '',
  role: 'Staff',
};

const EMPTY_ASSIGN: AssignExistingUserRequest = {
  email: '',
  role: 'Staff',
};

const AddTeamMemberModal: React.FC<Props> = ({
  show,
  onClose,
  onCreateUser,
  onAssignUser,
  isLoading,
  error,
  clearError,
}) => {
  const [tab, setTab] = useState<Tab>('create');
  const [createForm, setCreateForm] = useState<CreateTenantUserRequest>(EMPTY_CREATE);
  const [assignForm, setAssignForm] = useState<AssignExistingUserRequest>(EMPTY_ASSIGN);
  const [success, setSuccess] = useState<string | null>(null);

  const handleClose = () => {
    setCreateForm(EMPTY_CREATE);
    setAssignForm(EMPTY_ASSIGN);
    setSuccess(null);
    clearError();
    onClose();
  };

  const handleCreate = async () => {
    const result = await onCreateUser(createForm);
    if (result) {
      setSuccess(`${result.fullName} added successfully as ${result.role}.`);
      setCreateForm(EMPTY_CREATE);
    }
  };

  const handleAssign = async () => {
    const result = await onAssignUser(assignForm);
    if (result) {
      setSuccess(`${result.email} role updated to ${result.role}.`);
      setAssignForm(EMPTY_ASSIGN);
    }
  };

  const isCreateValid =
    createForm.fullName.trim() &&
    createForm.email.trim() &&
    createForm.role;

  const isAssignValid = assignForm.email.trim() && assignForm.role;

  if (!show) return null;

  return (
    <>
      <div
        className="modal-backdrop show"
        style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
        onClick={handleClose}
      />
      <div
        className="modal show d-block"
        style={{ zIndex: 1055 }}
        role="dialog"
        aria-modal="true"
      >
        <div className="modal-dialog modal-dialog-centered" style={{ maxWidth: 480 }}>
          <div className="modal-content border-0 shadow">
            <div className="modal-header border-bottom py-3">
              <h6 className="modal-title fw-bold">Add Team Member</h6>
              <button
                type="button"
                className="btn-close"
                onClick={handleClose}
                disabled={isLoading}
              />
            </div>

            {/* Tabs */}
            <div className="border-bottom">
              <div className="d-flex">
                <button
                  className={`btn btn-link px-4 py-3 text-decoration-none rounded-0 ${
                    tab === 'create'
                      ? 'border-bottom border-primary border-2 fw-semibold text-primary'
                      : 'text-muted'
                  }`}
                  style={{ fontSize: 14 }}
                  onClick={() => { setTab('create'); clearError(); setSuccess(null); }}
                >
                  <i className="fas fa-plus me-1" style={{ fontSize: 12 }} />
                  Create New
                </button>
                <button
                  className={`btn btn-link px-4 py-3 text-decoration-none rounded-0 ${
                    tab === 'assign'
                      ? 'border-bottom border-primary border-2 fw-semibold text-primary'
                      : 'text-muted'
                  }`}
                  style={{ fontSize: 14 }}
                  onClick={() => { setTab('assign'); clearError(); setSuccess(null); }}
                >
                  <i className="fas fa-user-check me-1" style={{ fontSize: 12 }} />
                  Assign Existing
                </button>
              </div>
            </div>

            <div className="modal-body p-4">
              {success && (
                <Alert variant="success" dismissible onDismiss={() => setSuccess(null)} className="mb-3">
                  {success}
                </Alert>
              )}
              {error && (
                <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
                  {error}
                </Alert>
              )}

              {tab === 'create' ? (
                <>
                  <div className="alert alert-info py-2 px-3 mb-3" style={{ fontSize: 13 }}>
                    <i className="fas fa-envelope me-2" />
                    An invitation email with a setup link will be sent to the user.
                  </div>
                  <div className="mb-3">
                    <label className="form-label small fw-medium">Full Name</label>
                    <input
                      type="text"
                      className="form-control"
                      value={createForm.fullName}
                      onChange={e => setCreateForm(p => ({ ...p, fullName: e.target.value }))}
                      placeholder="Jane Smith"
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Email</label>
                    <input
                      type="email"
                      className="form-control"
                      value={createForm.email}
                      onChange={e => setCreateForm(p => ({ ...p, email: e.target.value }))}
                      placeholder="jane@example.com"
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Role</label>
                    <select
                      className="form-select"
                      value={createForm.role}
                      onChange={e => setCreateForm(p => ({ ...p, role: e.target.value }))}
                    >
                      {ROLE_OPTIONS.map(r => (
                        <option key={r} value={r}>
                          {r === 'TenantAdmin' ? 'Admin' : r}
                        </option>
                      ))}
                    </select>
                  </div>
                </>
              ) : (
                <>
                  <p className="text-muted small mb-3">
                    Find an existing user in this organization and update their role.
                  </p>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">Email Address</label>
                    <input
                      type="email"
                      className="form-control"
                      value={assignForm.email}
                      onChange={e => setAssignForm(p => ({ ...p, email: e.target.value }))}
                      placeholder="user@example.com"
                    />
                  </div>

                  <div className="mb-3">
                    <label className="form-label small fw-medium">New Role</label>
                    <select
                      className="form-select"
                      value={assignForm.role}
                      onChange={e => setAssignForm(p => ({ ...p, role: e.target.value }))}
                    >
                      {ROLE_OPTIONS.map(r => (
                        <option key={r} value={r}>
                          {r === 'TenantAdmin' ? 'Admin' : r}
                        </option>
                      ))}
                    </select>
                  </div>
                </>
              )}
            </div>

            <div className="modal-footer border-top py-3">
              <button
                className="btn btn-outline-secondary btn-sm"
                onClick={handleClose}
                disabled={isLoading}
              >
                Cancel
              </button>
              <Button
                variant="primary"
                onClick={tab === 'create' ? handleCreate : handleAssign}
                loading={isLoading}
                disabled={tab === 'create' ? !isCreateValid : !isAssignValid}
              >
                {tab === 'create' ? 'Send Invite' : 'Assign User'}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default AddTeamMemberModal;
