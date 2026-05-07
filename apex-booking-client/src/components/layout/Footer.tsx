import React from 'react';

export const Footer: React.FC = () => {
  return (
    <footer className="bg-white border-top border-opacity-25 px-3 py-2" style={{ borderColor: 'rgba(0,0,0,0.08)' }}>
      <div className="d-flex justify-content-between align-items-center">
        <div className="small text-muted">
          © 2024 ApexBooking. All rights reserved.
        </div>
        <div className="small text-muted">
          Version 1.0.0
        </div>
      </div>
    </footer>
  );
};
