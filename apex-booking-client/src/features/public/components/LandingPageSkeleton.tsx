import React from 'react';
import './Skeleton.styles.css';

const LandingPageSkeleton: React.FC = () => (
  <div style={{ fontFamily: "'DM Sans', sans-serif", overflowX: 'hidden' }}>
    <div className="skeleton-nav">
      <div className="skeleton-block" style={{ width: 160, height: 24, borderRadius: 6 }} />
      <div style={{ display: 'flex', gap: 12 }}>
        <div className="skeleton-block" style={{ width: 80, height: 36, borderRadius: 10 }} />
        <div className="skeleton-block" style={{ width: 110, height: 36, borderRadius: 10 }} />
      </div>
    </div>

    {/* Hero */}
    <div style={{ minHeight: '100vh', background: 'linear-gradient(160deg, #f0f7ff 0%, #ffffff 50%, #f8faff 100%)', paddingTop: 64, display: 'flex', alignItems: 'center' }}>
      <div className="container py-5">
        <div className="row align-items-center g-5">
          <div className="col-lg-6">
            <div className="skeleton-block mb-4" style={{ width: 200, height: 28, borderRadius: 100 }} />
            <div className="skeleton-block mb-3" style={{ height: 56, maxWidth: 420, borderRadius: 8 }} />
            <div className="skeleton-block mb-4" style={{ height: 40, maxWidth: 320, borderRadius: 8 }} />
            <div className="skeleton-block mb-5" style={{ height: 72, maxWidth: 480, borderRadius: 8 }} />
            <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
              <div className="skeleton-block" style={{ width: 148, height: 48, borderRadius: 10 }} />
              <div className="skeleton-block" style={{ width: 100, height: 48, borderRadius: 10 }} />
            </div>
          </div>
          <div className="col-lg-6 d-none d-lg-block">
            <div className="skeleton-block" style={{ height: 300, borderRadius: 20 }} />
          </div>
        </div>
      </div>
    </div>

    {/* Industry strip */}
    <div style={{ height: 56, borderTop: '1px solid #e9ecef', borderBottom: '1px solid #e9ecef', background: 'white' }} />

    {/* Features */}
    <div style={{ padding: '80px 0', background: 'white' }}>
      <div className="container">
        <div className="text-center mb-5">
          <div className="skeleton-block mx-auto mb-3" style={{ width: 80, height: 20, borderRadius: 100 }} />
          <div className="skeleton-block mx-auto mb-3" style={{ width: 360, height: 44, borderRadius: 8 }} />
          <div className="skeleton-block mx-auto" style={{ width: 300, height: 20, borderRadius: 6 }} />
        </div>
        <div className="row g-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="col-md-6 col-lg-4">
              <div className="skeleton-block" style={{ height: 164, borderRadius: 16 }} />
            </div>
          ))}
        </div>
      </div>
    </div>
  </div>
);

export default LandingPageSkeleton;
