import React from 'react';
import './Skeleton.styles.css';

const RequestFormSkeleton: React.FC = () => (
  <div style={{ minHeight: '100vh', background: '#f8f9fa', fontFamily: "'DM Sans', sans-serif" }}>
    <div className="skeleton-nav">
      <div className="skeleton-block" style={{ width: 160, height: 24, borderRadius: 6 }} />
      <div className="skeleton-block" style={{ width: 80, height: 36, borderRadius: 10 }} />
    </div>

    <div className="container" style={{ paddingTop: 104, paddingBottom: 80 }}>
      <div style={{ background: 'white', border: '1px solid #e9ecef', borderRadius: 20, padding: '2.5rem', boxShadow: '0 4px 24px rgba(0,0,0,0.06)', maxWidth: 520, margin: '0 auto' }}>
        <div className="skeleton-block mb-3" style={{ width: 120, height: 24, borderRadius: 100 }} />
        <div className="skeleton-block mb-2" style={{ width: 200, height: 36, borderRadius: 8 }} />
        <div className="skeleton-block mb-4" style={{ width: 260, height: 18, borderRadius: 6 }} />

        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="mb-3">
            <div className="skeleton-block mb-2" style={{ width: 140, height: 16, borderRadius: 4 }} />
            <div className="skeleton-block" style={{ height: 44, borderRadius: 8 }} />
          </div>
        ))}

        <div className="skeleton-block mt-2" style={{ height: 48, borderRadius: 10 }} />
      </div>
    </div>
  </div>
);

export default RequestFormSkeleton;
