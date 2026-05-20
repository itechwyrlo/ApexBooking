import React, { useState } from 'react';
import { faPlus, faUserCheck } from '@fortawesome/free-solid-svg-icons';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { Tabs } from '../../../components/ui/Tabs';
import type { TabItem } from '../../../components/ui/Tabs';
import type { CreateTenantUserRequest, AssignExistingUserRequest, TenantUserDto } from '../types';

type Tab = 'create' | 'assign';

const TEAM_TABS: Array<TabItem<Tab>> = [
  { id: 'create', label: 'Create New', icon: faPlus },
  { id: 'assign', label: 'Assign Existing', icon: faUserCheck },
];

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
        onClick={handleClose}
      />
      <div
        className="modal show d-block"
        role="dialog"
        aria-modal="true"
      >
        <div className="modal-dialog modal-dialog-centered sa-add-team-modal">
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

            <div className="px-4">
              <Tabs
                tabs={TEAM_TABS}
                activeTab={tab}
                onChange={(id) => { setTab(id); clearError(); setSuccess(null); }}
              />
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
                  <div className="alert alert-info py-2 px-3 mb-3 small">
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
