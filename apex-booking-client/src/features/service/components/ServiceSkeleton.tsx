import React from 'react';

const ServiceSkeleton: React.FC = () => (
  <div className="p-3">
    {[...Array(4)].map((_, i) => (
      <div key={i} className="d-flex align-items-center gap-3 mb-3">
        <div className="apex-skeleton apex-svc-sk-name" />
        <div className="apex-skeleton apex-svc-sk-meta" />
        <div className="apex-skeleton apex-svc-sk-status ms-auto" />
      </div>
    ))}
  </div>
);

export { ServiceSkeleton };
