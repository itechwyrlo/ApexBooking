import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { useCreateOrganization } from '../hooks/useCreateOrganization';
import type { CreateOrganizationRequest } from '../types';

const EMPTY: CreateOrganizationRequest = {
  slug: '',
  businessName: '',
  ownerFullName: '',
  ownerEmail: '',
  ownerPhone: '',
  adminPassword: '',
};

const NewOrganizationPage: React.FC = () => {
  const navigate = useNavigate();
  const { createOrganization, isLoading, error, clearError } = useCreateOrganization();
  const [form, setForm] = useState<CreateOrganizationRequest>(EMPTY);
  const [showPassword, setShowPassword] = useState(false);

  const change = (field: keyof CreateOrganizationRequest, value: string) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const autoSlug = (name: string) =>
    name
      .toLowerCase()
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .slice(0, 50);

  const handleBusinessNameChange = (value: string) => {
    change('businessName', value);
    if (!form.slug || form.slug === autoSlug(form.businessName)) {
      change('slug', autoSlug(value));
    }
  };

  const isValid =
    form.slug.trim() &&
    form.businessName.trim() &&
    form.ownerFullName.trim() &&
    form.ownerEmail.trim() &&
    form.ownerPhone.trim() &&
    form.adminPassword.trim();

  const handleSubmit = async () => {
    const result = await createOrganization(form);
    if (result) {
      navigate(`/superadmin/organizations/${result.slug}`);
    }
  };

  return (
    <div className="container-fluid">
      <div className="mb-4 d-flex align-items-center gap-3">
        <button
          className="btn btn-link p-0 text-muted"
          onClick={() => navigate('/superadmin/organizations')}
        >
          <i className="fas fa-arrow-left" />
        </button>
        <div>
          <h4 className="fw-bold mb-0">New Organization</h4>
          <div className="text-muted small">Create a new organization and its initial admin account.</div>
        </div>
      </div>

      {error && (
        <Alert variant="error" dismissible onDismiss={clearError} className="mb-4">
          {error}
        </Alert>
      )}

      <div className="row g-4">
        {/* Organization Details */}
        <div className="col-lg-6">
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white py-3">
              <span className="fw-semibold">Organization Details</span>
            </div>
            <div className="card-body p-4">
              <div className="mb-3">
                <label className="form-label small fw-medium">Organization Name</label>
                <input
                  type="text"
                  className="form-control"
                  value={form.businessName}
                  onChange={e => handleBusinessNameChange(e.target.value)}
                  placeholder="Acme Corp"
                />
              </div>

              <div className="mb-3">
                <label className="form-label small fw-medium">Booking URL Slug</label>
                <div className="input-group">
                  <span className="input-group-text text-muted small">/book/</span>
                  <input
                    type="text"
                    className="form-control"
                    value={form.slug}
                    onChange={e => change('slug', e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
                    placeholder="acme-corp"
                  />
                </div>
                <div className="form-text">Lowercase letters, digits, and hyphens only.</div>
              </div>
            </div>
          </div>
        </div>

        {/* Admin Account */}
        <div className="col-lg-6">
          <div className="card border-0 shadow-sm">
            <div className="card-header bg-white py-3">
              <span className="fw-semibold">Admin Account</span>
            </div>
            <div className="card-body p-4">
              <div className="mb-3">
                <label className="form-label small fw-medium">Full Name</label>
                <input
                  type="text"
                  className="form-control"
                  value={form.ownerFullName}
                  onChange={e => change('ownerFullName', e.target.value)}
                  placeholder="Jane Smith"
                />
              </div>

              <div className="mb-3">
                <label className="form-label small fw-medium">Email</label>
                <input
                  type="email"
                  className="form-control"
                  value={form.ownerEmail}
                  onChange={e => change('ownerEmail', e.target.value)}
                  placeholder="jane@acme.com"
                />
              </div>

              <div className="mb-3">
                <label className="form-label small fw-medium">Phone</label>
                <input
                  type="tel"
                  className="form-control"
                  value={form.ownerPhone}
                  onChange={e => change('ownerPhone', e.target.value)}
                  placeholder="+1234567890"
                />
              </div>

              <div className="mb-3">
                <label className="form-label small fw-medium">Temporary Password</label>
                <div className="input-group">
                  <input
                    type={showPassword ? 'text' : 'password'}
                    className="form-control"
                    value={form.adminPassword}
                    onChange={e => change('adminPassword', e.target.value)}
                    placeholder="Min 8 chars, 1 uppercase, 1 digit"
                  />
                  <button
                    className="btn btn-outline-secondary"
                    type="button"
                    onClick={() => setShowPassword(p => !p)}
                  >
                    <i className={`fas ${showPassword ? 'fa-eye-slash' : 'fa-eye'}`} />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="mt-4 d-flex gap-2">
        <Button
          variant="primary"
          onClick={handleSubmit}
          loading={isLoading}
          disabled={!isValid}
        >
          Create Organization
        </Button>
        <button
          className="btn btn-outline-secondary"
          onClick={() => navigate('/superadmin/organizations')}
          disabled={isLoading}
        >
          Cancel
        </button>
      </div>
    </div>
  );
};

export default NewOrganizationPage;
