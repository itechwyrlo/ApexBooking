import React from 'react';
import { useNavigate } from 'react-router-dom';

const PLANS = [
  {
    name: 'Basic',
    price: '$19',
    period: '/mo',
    description: 'Everything you need to get started.',
    features: [
      '3 staff members',
      '5 services',
      '100 bookings / month',
      'Public booking page',
      'Email notifications',
      'Online payments',
    ],
    cta: 'Get Started',
    highlighted: false,
  },
  {
    name: 'Professional',
    price: '$49',
    period: '/mo',
    description: 'Scale without limits.',
    features: [
      '10 staff members',
      '20 services',
      'Unlimited bookings',
      'Public booking page',
      'Email notifications',
      'Online payments',
      'Priority support',
    ],
    cta: 'Get Started',
    highlighted: true,
  },
];

const PricingPage: React.FC = () => {
  const navigate = useNavigate();

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
        .pricing-card {
          background: white; border: 1.5px solid var(--apex-border); border-radius: 20px;
          padding: 2.5rem; transition: all 0.2s; height: 100%;
        }
        .pricing-card.featured {
          border-color: var(--apex-blue);
          box-shadow: 0 8px 40px rgba(13,110,253,0.15);
        }
        .plan-price {
          font-family: 'DM Serif Display', serif; font-size: 3.5rem;
          color: var(--apex-text); letter-spacing: -0.04em; line-height: 1;
        }
        .plan-feature {
          display: flex; align-items: center; gap: 10px;
          font-size: 0.9rem; color: var(--apex-muted); margin-bottom: 0.6rem;
        }
        .plan-feature i { color: var(--apex-blue); font-size: 0.8rem; }
        .btn-plan {
          width: 100%; padding: 14px; border-radius: 10px; font-size: 0.95rem;
          font-weight: 600; border: none; cursor: pointer; transition: all 0.2s;
        }
        .btn-plan-primary { background: var(--apex-blue); color: white; }
        .btn-plan-primary:hover { background: var(--apex-blue-dark); transform: translateY(-1px); box-shadow: 0 6px 20px rgba(13,110,253,0.3); }
        .btn-plan-outline { background: white; color: var(--apex-text); border: 1.5px solid var(--apex-border) !important; }
        .btn-plan-outline:hover { border-color: var(--apex-blue) !important; color: var(--apex-blue); background: var(--apex-blue-soft); }
      `}</style>

      <nav className="apex-nav">
        <a href="/" className="apex-logo">
          <div className="apex-logo-icon"><i className="fas fa-calendar-check" /></div>
          <span className="apex-logo-text">ApexBooking</span>
        </a>
        <div className="d-flex align-items-center gap-3">
          <a href="/login" style={{ color: 'var(--apex-text)', textDecoration: 'none', fontSize: '0.875rem', fontWeight: 500 }}>
            Sign In
          </a>
        </div>
      </nav>

      <div className="container" style={{ paddingTop: 112, paddingBottom: 80 }}>
        <div className="text-center mb-5">
          <div style={{ display: 'inline-flex', alignItems: 'center', gap: 8, background: 'var(--apex-blue-soft)', color: 'var(--apex-blue)', padding: '6px 14px', borderRadius: 100, fontSize: '0.8rem', fontWeight: 600, marginBottom: '1.25rem', letterSpacing: '0.04em', textTransform: 'uppercase' }}>
            <i className="fas fa-circle" style={{ fontSize: 6 }} />
            Pricing
          </div>
          <h1 style={{ fontFamily: "'DM Serif Display', serif", fontSize: 'clamp(2.2rem, 5vw, 3.5rem)', color: 'var(--apex-text)', letterSpacing: '-0.03em', lineHeight: 1.1, marginBottom: '1rem' }}>
            Simple, transparent pricing
          </h1>
          <p style={{ fontSize: '1.05rem', color: 'var(--apex-muted)', maxWidth: 480, margin: '0 auto' }}>
            Start your free trial — no credit card required. Upgrade or cancel any time.
          </p>
        </div>

        <div className="row g-4 justify-content-center" style={{ maxWidth: 820, margin: '0 auto' }}>
          {PLANS.map(plan => (
            <div key={plan.name} className="col-md-6">
              <div className={`pricing-card${plan.highlighted ? ' featured' : ''}`}>
                {plan.highlighted && (
                  <div style={{ background: 'var(--apex-blue)', color: 'white', fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase', padding: '4px 12px', borderRadius: 100, display: 'inline-block', marginBottom: '1.25rem' }}>
                    Most Popular
                  </div>
                )}
                <div className="mb-1" style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--apex-text)' }}>{plan.name}</div>
                <div className="mb-1">
                  <span className="plan-price">{plan.price}</span>
                  <span style={{ color: 'var(--apex-muted)', fontWeight: 500, marginLeft: 4 }}>{plan.period}</span>
                </div>
                <div style={{ fontSize: '0.875rem', color: 'var(--apex-muted)', marginBottom: '1.75rem' }}>{plan.description}</div>

                <div className="mb-4">
                  {plan.features.map(f => (
                    <div key={f} className="plan-feature">
                      <i className="fas fa-check-circle" />
                      {f}
                    </div>
                  ))}
                </div>

                <button
                  className={`btn-plan ${plan.highlighted ? 'btn-plan-primary' : 'btn-plan-outline'}`}
                  onClick={() => navigate(`/request-access?plan=${plan.name}`)}
                >
                  {plan.cta}
                </button>
              </div>
            </div>
          ))}
        </div>

        <div className="text-center mt-5">
          <p style={{ color: 'var(--apex-muted)', fontSize: '0.875rem' }}>
            Already have an account?{' '}
            <a href="/login" style={{ color: 'var(--apex-blue)', fontWeight: 600, textDecoration: 'none' }}>Sign in</a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default PricingPage;
