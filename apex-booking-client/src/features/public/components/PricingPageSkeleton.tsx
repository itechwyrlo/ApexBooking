import React from 'react';
import './Skeleton.styles.css';

const PricingPageSkeleton: React.FC = () => (
  <div style={{ minHeight: '100vh', background: '#f8f9fa', fontFamily: "'DM Sans', sans-serif" }}>
    <div className="skeleton-nav">
      <div className="skeleton-block" style={{ width: 160, height: 24, borderRadius: 6 }} />
      <div className="skeleton-block" style={{ width: 80, height: 36, borderRadius: 10 }} />
    </div>

    <div className="container" style={{ paddingTop: 112, paddingBottom: 80 }}>
      <div className="text-center mb-5">
        <div className="skeleton-block mx-auto mb-3" style={{ width: 80, height: 24, borderRadius: 100 }} />
        <div className="skeleton-block mx-auto mb-3" style={{ width: 360, height: 52, borderRadius: 8 }} />
        <div className="skeleton-block mx-auto" style={{ width: 300, height: 22, borderRadius: 6 }} />
      </div>

      <div className="row g-4 justify-content-center" style={{ maxWidth: 820, margin: '0 auto' }}>
        {[0, 1].map(i => (
          <div key={i} className="col-md-6">
            <div className="skeleton-block" style={{ height: 440, borderRadius: 20 }} />
          </div>
        ))}
      </div>
    </div>
  </div>
);

export default PricingPageSkeleton;
