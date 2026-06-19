import React from 'react';

export const Footer: React.FC = () => {
  return (
    <footer className="bg-white border-top d-flex align-items-center justify-content-between px-4 flex-shrink-0" style={{ height: '40px' }}>
      <span className="small text-muted">© 2024 ApexBooking. All rights reserved.</span>
      <span className="small text-muted">Version 1.0.0</span>
    </footer>
  );
};
