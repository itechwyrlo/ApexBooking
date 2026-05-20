import React from 'react';
import { useNavigate } from 'react-router-dom';
import PublicNav from '../components/PublicNav';

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
    <div className="min-vh-100 bg-light">
      <PublicNav />

      <div className="container pricing-page-body">
        <div className="text-center mb-5">
          <div className="pricing-badge">
            <i className="fas fa-circle" />
            Pricing
          </div>
          <h1 className="pricing-headline">Simple, transparent pricing</h1>
          <p className="pricing-subtitle">
            Start your free trial — no credit card required. Upgrade or cancel any time.
          </p>
        </div>

        <div className="row g-4 justify-content-center pricing-row-wrap">
          {PLANS.map(plan => (
            <div key={plan.name} className="col-md-6">
              <div className={`pricing-card${plan.highlighted ? ' featured' : ''}`}>
                {plan.highlighted && (
                  <div className="pricing-popular-badge">Most Popular</div>
                )}
                <div className="fw-bold mb-1">{plan.name}</div>
                <div className="mb-1">
                  <span className="plan-price">{plan.price}</span>
                  <span className="plan-period">{plan.period}</span>
                </div>
                <p className="plan-desc">{plan.description}</p>

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
          <p className="small text-muted">
            Already have an account?{' '}
            <a href="/login" className="link-primary fw-semibold">Sign in</a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default PricingPage;
