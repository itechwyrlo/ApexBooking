import React from 'react';

const StaffSkeleton: React.FC = () => (
  <div className="p-3">
    {[...Array(4)].map((_, i) => (
      <div key={i} className="d-flex align-items-center gap-3 mb-3">
        <div className="apex-skeleton apex-staff-sk-name" />
        <div className="apex-skeleton apex-staff-sk-desc" />
        <div className="apex-skeleton apex-staff-sk-status ms-auto" />
      </div>
    ))}
  </div>
);

export { StaffSkeleton };
