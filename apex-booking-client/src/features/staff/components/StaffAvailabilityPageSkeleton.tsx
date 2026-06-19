import React from 'react';

const StaffAvailabilityPageSkeleton: React.FC<{ showBackButton?: boolean }> = ({ showBackButton = true }) => (
  <div className="container-fluid px-3 px-md-4 py-4">
    <div className="row mb-4 align-items-center">
      <div className="col d-flex align-items-center gap-3">
        {showBackButton && (
          <span className="apex-skeleton d-block" style={{ width: 62, height: 32, borderRadius: 6 }} />
        )}
        <div>
          <span className="apex-skeleton d-block mb-1" style={{ width: 160, height: 18 }} />
          <span className="apex-skeleton d-block" style={{ width: 300, height: 12 }} />
        </div>
      </div>
    </div>

    <div className="card border-0 shadow-sm mb-4">
      <div className="card-header bg-white border-bottom py-3">
        <span className="apex-skeleton d-block mb-2" style={{ width: 140, height: 15 }} />
        <span className="apex-skeleton d-block" style={{ width: 340, height: 12 }} />
      </div>
      <div className="card-body p-0">
        {[...Array(7)].map((_, i) => (
          <div key={i} className="px-4 py-3 border-bottom d-flex align-items-center gap-3 flex-wrap">
            <span className="apex-skeleton d-block" style={{ width: 100, height: 16 }} />
            <span className="apex-skeleton d-block" style={{ width: 120, height: 32, borderRadius: 6 }} />
            <span className="apex-skeleton d-block" style={{ width: 18, height: 13 }} />
            <span className="apex-skeleton d-block" style={{ width: 120, height: 32, borderRadius: 6 }} />
            <span className="apex-skeleton d-block ms-auto" style={{ width: 96, height: 32, borderRadius: 6 }} />
          </div>
        ))}
      </div>
      <div className="card-footer bg-white d-flex justify-content-end py-3">
        <span className="apex-skeleton d-block" style={{ width: 130, height: 36, borderRadius: 8 }} />
      </div>
    </div>

    <div className="card border-0 shadow-sm">
      <div className="card-header bg-white border-bottom py-3 d-flex justify-content-between align-items-center">
        <div>
          <span className="apex-skeleton d-block mb-2" style={{ width: 120, height: 15 }} />
          <span className="apex-skeleton d-block" style={{ width: 260, height: 12 }} />
        </div>
        <span className="apex-skeleton d-block" style={{ width: 116, height: 32, borderRadius: 6 }} />
      </div>
      <div className="card-body p-4 text-center">
        <span className="apex-skeleton d-inline-block" style={{ width: 160, height: 13 }} />
      </div>
    </div>
  </div>
);

export { StaffAvailabilityPageSkeleton };
