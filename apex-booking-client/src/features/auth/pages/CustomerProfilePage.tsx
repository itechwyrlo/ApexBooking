import React from 'react';
import { useAuth } from '../../../context/AuthContext';

const CustomerProfilePage: React.FC = () => {
  const { user } = useAuth();

  return (
    <div className="container-fluid">
      <div className="mb-4">
        <h4 className="fw-bold mb-1">Profile</h4>
        <div className="text-muted small">View your account information.</div>
      </div>

      <div className="card border-0 shadow-sm" style={{ maxWidth: 560 }}>
        <div className="card-body p-4">
          <h6 className="fw-semibold mb-3">Account Information</h6>

          <div className="mb-3">
            <label className="form-label small fw-medium text-muted">Full Name</label>
            <div className="form-control bg-light">{user?.fullName || 'N/A'}</div>
          </div>

          <div className="mb-3">
            <label className="form-label small fw-medium text-muted">Email</label>
            <div className="form-control bg-light">{user?.email || 'N/A'}</div>
          </div>

          <div className="mb-3">
            <label className="form-label small fw-medium text-muted">Role</label>
            <div className="form-control bg-light">{user?.role || 'N/A'}</div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CustomerProfilePage;
