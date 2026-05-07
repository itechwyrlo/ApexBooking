import React from 'react';

interface HeaderProps {
  onToggleSidebar: () => void;
}

export const Header: React.FC<HeaderProps> = ({ onToggleSidebar }) => {
  return (
    <header className="bg-white border-bottom border-opacity-25 px-3 py-2" style={{ height: '56px', borderColor: 'rgba(0,0,0,0.08)' }}>
      <div className="d-flex align-items-center justify-content-between h-100">
        <button
          onClick={onToggleSidebar}
          className="p-2 text-secondary hover:text-dark hover:bg-light rounded transition-colors border-0 bg-transparent d-flex align-items-center justify-content-center"
          style={{ width: '40px', height: '40px' }}
        >
          <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        
        <div className="d-flex align-items-center gap-3">
          <h1 className="fs-6 fw-medium text-dark mb-0">Dashboard</h1>
        </div>
      </div>
    </header>
  );
};
