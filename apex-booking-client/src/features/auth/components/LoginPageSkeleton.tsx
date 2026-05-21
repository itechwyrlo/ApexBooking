import React from 'react';
import '../../public/components/Skeleton.styles.css';

const LoginPageSkeleton: React.FC = () => (
  <div style={{
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: 'linear-gradient(160deg, #f0f7ff 0%, #ffffff 60%, #f8faff 100%)',
    padding: '2rem 0',
    fontFamily: "'DM Sans', sans-serif",
  }}>
    <div style={{
      background: 'white',
      border: '1px solid #e9ecef',
      borderRadius: 20,
      padding: '2.5rem',
      boxShadow: '0 4px 32px rgba(0,0,0,0.07)',
      width: '100%',
      maxWidth: 420,
      margin: '0 1rem',
    }}>
      {/* Brand */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: '1.75rem' }}>
        <div className="skeleton-block" style={{ width: 36, height: 36, borderRadius: 10, flexShrink: 0 }} />
        <div className="skeleton-block" style={{ width: 130, height: 18, borderRadius: 6 }} />
      </div>

      {/* Heading */}
      <div className="skeleton-block" style={{ width: 190, height: 32, borderRadius: 8, marginBottom: '0.5rem' }} />
      {/* Subheading */}
      <div className="skeleton-block" style={{ width: 260, height: 16, borderRadius: 6, marginBottom: '1.75rem' }} />

      {/* Email label + input */}
      <div className="skeleton-block" style={{ width: 110, height: 14, borderRadius: 4, marginBottom: '0.5rem' }} />
      <div className="skeleton-block" style={{ height: 44, borderRadius: 8, marginBottom: '1rem' }} />

      {/* Password label + input */}
      <div className="skeleton-block" style={{ width: 90, height: 14, borderRadius: 4, marginBottom: '0.5rem' }} />
      <div className="skeleton-block" style={{ height: 44, borderRadius: 8, marginBottom: '1rem' }} />

      {/* Remember me + forgot password */}
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1.5rem' }}>
        <div className="skeleton-block" style={{ width: 110, height: 16, borderRadius: 4 }} />
        <div className="skeleton-block" style={{ width: 120, height: 16, borderRadius: 4 }} />
      </div>

      {/* Sign in button */}
      <div className="skeleton-block" style={{ height: 48, borderRadius: 10, marginBottom: '1.25rem' }} />

      {/* Request access */}
      <div className="skeleton-block" style={{ width: 200, height: 14, borderRadius: 4, margin: '0 auto' }} />
    </div>
  </div>
);

export default LoginPageSkeleton;
