import React from 'react';

interface Props {
  showGetStarted?: boolean;
}

const PublicNav: React.FC<Props> = ({ showGetStarted }) => (
  <nav className="public-nav">
    <a href="/" className="apex-logo">
      <img src="/apexbooking-logo.svg" alt="ApexBooking" style={{ width: 36, height: 36, borderRadius: 10 }} />
      <span className="apex-logo-text">ApexBooking</span>
    </a>
    <div className="d-flex align-items-center gap-3">
      {showGetStarted ? (
        <>
          <a href="/login" className="btn-apex-ghost btn-apex-sm">Sign In</a>
          <a href="/pricing" className="btn-apex-primary btn-apex-sm">Get Started</a>
        </>
      ) : (
        <a href="/login" className="apex-nav-link">Sign In</a>
      )}
    </div>
  </nav>
);

export default PublicNav;
