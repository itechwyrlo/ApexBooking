import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTenantRequest } from '../hooks/useTenantRequest';
import PublicNav from '../components/PublicNav';

const VALID_PLANS = ['Basic', 'Professional'] as const;
type Plan = typeof VALID_PLANS[number];

const coercePlan = (raw: string | null): Plan =>
  VALID_PLANS.includes(raw as Plan) ? (raw as Plan) : 'Basic';

const RequestAccessPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const plan = coercePlan(searchParams.get('plan'));

  const { submitRequest, isLoading, isSuccess, error, clearError } = useTenantRequest();

  const [businessName, setBusinessName] = useState('');
  const [ownerFullName, setOwnerFullName] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');
  const [ownerPhone, setOwnerPhone] = useState('');
  const [message, setMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    clearError();
    await submitRequest({ businessName, ownerFullName, ownerEmail, ownerPhone, plan, message: message || undefined });
  };

  return (
    <div className="min-vh-100 bg-light">
      <PublicNav />

      <div className="container request-page-body">
        {isSuccess ? (
          <div className="request-card text-center">
            <div className="request-success-icon">
              <i className="fas fa-check" />
            </div>
            <h5 className="request-success-title">Request received</h5>
            <p className="text-muted mb-4" style={{ fontSize: '0.95rem', lineHeight: 1.65 }}>
              Thanks for your interest in ApexBooking. We'll review your request and send setup instructions to <strong>{ownerEmail}</strong> within 1–2 business days.
            </p>
            <a href="/" className="link-primary fw-semibold small">
              ← Back to home
            </a>
          </div>
        ) : (
          <div className="request-card">
            <div className="mb-4">
              <div className="request-plan-badge">
                {plan} Plan
                <a href="/pricing" className="text-primary ms-1 fw-semibold" style={{ fontSize: '0.75rem' }}>Change</a>
              </div>
              <h2 className="request-title">Request access</h2>
              <p className="text-muted small" style={{ lineHeight: 1.65 }}>
                ApexBooking is an invitation-only platform. Submit your details and our team will review your request within 1–2 business days.
              </p>
            </div>

            {error && (
              <div className="alert alert-danger small mb-4 py-2">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <div className="field-label">Business name <span className="text-danger">*</span></div>
                <input
                  className="field-input"
                  type="text"
                  placeholder="Lumiere Salon"
                  required
                  value={businessName}
                  onChange={e => setBusinessName(e.target.value)}
                />
              </div>
              <div className="mb-3">
                <div className="field-label">Your full name <span className="text-danger">*</span></div>
                <input
                  className="field-input"
                  type="text"
                  placeholder="Jane Smith"
                  required
                  value={ownerFullName}
                  onChange={e => setOwnerFullName(e.target.value)}
                />
              </div>
              <div className="mb-3">
                <div className="field-label">Email address <span className="text-danger">*</span></div>
                <input
                  className="field-input"
                  type="email"
                  placeholder="jane@lumieresalon.com"
                  required
                  value={ownerEmail}
                  onChange={e => setOwnerEmail(e.target.value)}
                />
              </div>
              <div className="mb-3">
                <div className="field-label">Phone number <span className="text-danger">*</span></div>
                <input
                  className="field-input"
                  type="tel"
                  placeholder="+1 555 000 0000"
                  required
                  value={ownerPhone}
                  onChange={e => setOwnerPhone(e.target.value)}
                />
              </div>
              <div className="mb-4">
                <div className="field-label">
                  Message <span className="text-muted fw-normal">(optional)</span>
                </div>
                <textarea
                  className="field-input field-textarea"
                  placeholder="Tell us a bit about your business..."
                  rows={3}
                  value={message}
                  onChange={e => setMessage(e.target.value)}
                />
              </div>

              <button type="submit" className="btn-submit" disabled={isLoading}>
                {isLoading ? 'Submitting...' : 'Submit Request'}
              </button>
            </form>
          </div>
        )}
      </div>
    </div>
  );
};

export default RequestAccessPage;
