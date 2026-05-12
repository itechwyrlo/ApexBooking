import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTenantRequest } from '../hooks/useTenantRequest';

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    clearError();
    await submitRequest({ businessName, ownerFullName, ownerEmail, ownerPhone, plan, message: message || undefined });
  };

  return (
    <div style={{ fontFamily: "'DM Sans', sans-serif", minHeight: '100vh', background: '#f8fafc' }}>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,300;0,9..40,400;0,9..40,500;0,9..40,600;0,9..40,700;0,9..40,800&family=DM+Serif+Display:ital@0;1&display=swap');
        :root {
          --apex-blue: #0d6efd;
          --apex-blue-dark: #0a58ca;
          --apex-blue-soft: #e8f0fe;
          --apex-text: #0f172a;
          --apex-muted: #64748b;
          --apex-border: #e2e8f0;
        }
        .apex-nav {
          position: fixed; top: 0; left: 0; right: 0; z-index: 1000;
          background: rgba(255,255,255,0.92); backdrop-filter: blur(12px);
          border-bottom: 1px solid var(--apex-border);
          padding: 0 2rem; height: 64px;
          display: flex; align-items: center; justify-content: space-between;
        }
        .apex-logo { display: flex; align-items: center; gap: 10px; text-decoration: none; }
        .apex-logo-icon {
          width: 36px; height: 36px; background: var(--apex-blue); border-radius: 10px;
          display: flex; align-items: center; justify-content: center; font-size: 16px; color: white;
        }
        .apex-logo-text {
          font-family: 'DM Serif Display', serif; font-size: 1.25rem;
          color: var(--apex-text); font-weight: 400; letter-spacing: -0.02em;
        }
        .request-card {
          background: white; border: 1px solid var(--apex-border); border-radius: 20px;
          padding: 2.5rem; box-shadow: 0 4px 24px rgba(0,0,0,0.06);
          max-width: 520px; margin: 0 auto;
        }
        .field-label { font-size: 0.875rem; font-weight: 600; color: var(--apex-text); margin-bottom: 6px; }
        .field-input {
          width: 100%; padding: 10px 14px; border: 1.5px solid var(--apex-border);
          border-radius: 8px; font-size: 0.9rem; font-family: inherit; outline: none;
          transition: border-color 0.15s;
        }
        .field-input:focus { border-color: var(--apex-blue); }
        .btn-submit {
          width: 100%; padding: 14px; background: var(--apex-blue); color: white;
          border: none; border-radius: 10px; font-size: 0.95rem; font-weight: 600;
          cursor: pointer; transition: all 0.2s; font-family: inherit;
        }
        .btn-submit:hover:not(:disabled) { background: var(--apex-blue-dark); transform: translateY(-1px); box-shadow: 0 6px 20px rgba(13,110,253,0.3); }
        .btn-submit:disabled { opacity: 0.65; cursor: not-allowed; }
      `}</style>

      <nav className="apex-nav">
        <a href="/" className="apex-logo">
          <div className="apex-logo-icon"><i className="fas fa-calendar-check" /></div>
          <span className="apex-logo-text">ApexBooking</span>
        </a>
        <a href="/login" style={{ color: 'var(--apex-text)', textDecoration: 'none', fontSize: '0.875rem', fontWeight: 500 }}>
          Sign In
        </a>
      </nav>

      <div className="container" style={{ paddingTop: 104, paddingBottom: 80 }}>
        {isSuccess ? (
          <div className="request-card text-center">
            <div style={{ width: 64, height: 64, background: '#f0fdf4', borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 1.5rem' }}>
              <i className="fas fa-check" style={{ color: '#16a34a', fontSize: 28 }} />
            </div>
            <h5 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.5rem', color: 'var(--apex-text)', marginBottom: '0.75rem' }}>
              Request received
            </h5>
            <p style={{ color: 'var(--apex-muted)', fontSize: '0.95rem', lineHeight: 1.65, marginBottom: '1.5rem' }}>
              Thanks for your interest in ApexBooking. We'll review your request and send setup instructions to <strong>{ownerEmail}</strong> within 1–2 business days.
            </p>
            <a href="/" style={{ color: 'var(--apex-blue)', fontWeight: 600, textDecoration: 'none', fontSize: '0.9rem' }}>
              ← Back to home
            </a>
          </div>
        ) : (
          <div className="request-card">
            <div className="mb-4">
              <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, background: 'var(--apex-blue-soft)', color: 'var(--apex-blue)', padding: '5px 12px', borderRadius: 100, fontSize: '0.78rem', fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase', marginBottom: '1rem' }}>
                {plan} Plan
                <a href="/pricing" style={{ color: 'var(--apex-blue)', marginLeft: 4, fontSize: '0.75rem', fontWeight: 600 }}>Change</a>
              </div>
              <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: '1.75rem', color: 'var(--apex-text)', letterSpacing: '-0.02em', marginBottom: '0.4rem' }}>
                Request access
              </h2>
              <p style={{ color: 'var(--apex-muted)', fontSize: '0.9rem' }}>
                Fill in your details and we'll get you set up quickly.
              </p>
            </div>

            {error && (
              <div className="alert alert-danger small mb-4 py-2">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <div className="field-label">Business name <span style={{ color: '#ef4444' }}>*</span></div>
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
                <div className="field-label">Your full name <span style={{ color: '#ef4444' }}>*</span></div>
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
                <div className="field-label">Email address <span style={{ color: '#ef4444' }}>*</span></div>
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
                <div className="field-label">Phone number <span style={{ color: '#ef4444' }}>*</span></div>
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
                <div className="field-label">Message <span style={{ color: 'var(--apex-muted)', fontWeight: 400 }}>(optional)</span></div>
                <textarea
                  className="field-input"
                  placeholder="Tell us a bit about your business..."
                  rows={3}
                  style={{ resize: 'vertical' }}
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
