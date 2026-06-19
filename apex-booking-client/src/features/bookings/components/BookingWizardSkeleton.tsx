import React from 'react';
import './BookingWizardSkeleton.styles.css';

const BookingWizardSkeleton: React.FC = () => (
  <div className="min-vh-100 bg-light py-4 px-3">
    <div className="apex-wiz-sk-inner">
      <div className="text-center mb-4">
        <div className="apex-skeleton apex-wiz-sk-logo mx-auto mb-2" />
        <div className="apex-skeleton apex-wiz-sk-title mx-auto mb-2" />
        <div className="apex-skeleton apex-wiz-sk-subtitle mx-auto" />
      </div>
      <div className="apex-skeleton apex-wiz-sk-progress mb-4" />
      <div className="card border-0 shadow-sm">
        <div className="card-header bg-white border-bottom py-3">
          <div className="apex-skeleton apex-wiz-sk-card-title mb-2" />
          <div className="apex-skeleton apex-wiz-sk-card-sub" />
        </div>
        <div className="card-body p-0">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="d-flex justify-content-between align-items-center px-4 py-3 border-bottom">
              <div className="d-flex flex-column gap-1">
                <div className="apex-skeleton apex-wiz-sk-svc-name" />
                <div className="apex-skeleton apex-wiz-sk-svc-dur" />
              </div>
              <div className="apex-skeleton apex-wiz-sk-svc-price" />
            </div>
          ))}
        </div>
      </div>
    </div>
  </div>
);

export { BookingWizardSkeleton };
