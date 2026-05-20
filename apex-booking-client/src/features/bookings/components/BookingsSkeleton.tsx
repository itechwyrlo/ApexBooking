import React from 'react';
import './BookingsSkeleton.styles.css';

const BookingsSkeleton: React.FC = () => (
  <div className="p-3">
    {[...Array(4)].map((_, i) => (
      <div key={i} className="d-flex align-items-center gap-3 mb-3">
        <div className="d-flex flex-column gap-1">
          <div className="apex-skeleton apex-bk-sk-name" />
          <div className="apex-skeleton apex-bk-sk-email" />
        </div>
        <div className="apex-skeleton apex-bk-sk-service" />
        <div className="apex-skeleton apex-bk-sk-staff" />
        <div className="d-flex flex-column gap-1">
          <div className="apex-skeleton apex-bk-sk-date" />
          <div className="apex-skeleton apex-bk-sk-time" />
        </div>
        <div className="apex-skeleton apex-bk-sk-badge ms-auto" />
        <div className="apex-skeleton apex-bk-sk-badge" />
      </div>
    ))}
  </div>
);

export { BookingsSkeleton };
