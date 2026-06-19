import React from 'react';
import './CustomerBookingsSkeleton.styles.css';

const CustomerBookingsSkeleton: React.FC = () => (
  <div className="p-3">
    {[...Array(4)].map((_, i) => (
      <div key={i} className="d-flex align-items-center gap-3 mb-3">
        <div className="apex-skeleton apex-cbk-sk-ref" />
        <div className="apex-skeleton apex-cbk-sk-service" />
        <div className="apex-skeleton apex-cbk-sk-staff" />
        <div className="apex-skeleton apex-cbk-sk-schedule" />
        <div className="apex-skeleton apex-cbk-sk-badge" />
        <div className="apex-skeleton apex-cbk-sk-price" />
        <div className="apex-skeleton apex-cbk-sk-action ms-auto" />
      </div>
    ))}
  </div>
);

export { CustomerBookingsSkeleton };
