import React from 'react';

export const DashboardSkeleton: React.FC = () => (
  <>
    <span className="apex-skeleton d-block mb-2" style={{ height: 22, width: 180 }} />
    <span className="apex-skeleton d-block mb-4" style={{ height: 13, width: 260 }} />

    <div className="row g-3 mb-4">
      {[0, 1, 2, 3].map(i => (
        <div key={i} className="col-6 col-lg-3">
          <div className="card border shadow-sm rounded p-3">
            <span className="apex-skeleton d-block mb-2" style={{ height: 11, width: '60%' }} />
            <span className="apex-skeleton d-block mb-2" style={{ height: 30, width: '50%' }} />
            <span className="apex-skeleton d-block" style={{ height: 11, width: '70%' }} />
          </div>
        </div>
      ))}
    </div>

    <div className="row g-3 mb-3">
      <div className="col-12 col-lg-6">
        <div className="card border shadow-sm rounded p-3">
          <span className="apex-skeleton d-block mb-3" style={{ height: 13, width: 140 }} />
          <span className="apex-skeleton d-block mb-2" style={{ height: 100 }} />
          <span className="apex-skeleton d-block" style={{ height: 11 }} />
        </div>
      </div>
      <div className="col-12 col-lg-6">
        <div className="card border shadow-sm rounded p-3">
          <span className="apex-skeleton d-block mb-3" style={{ height: 13, width: 140 }} />
          {[0, 1, 2, 3].map(i => (
            <div key={i} className="mb-3">
              <span className="apex-skeleton d-block mb-1" style={{ height: 11, width: '75%' }} />
              <span className="apex-skeleton d-block" style={{ height: 4 }} />
            </div>
          ))}
        </div>
      </div>
    </div>

    <div className="card border shadow-sm rounded p-3">
      <span className="apex-skeleton d-block mb-3" style={{ height: 13, width: 150 }} />
      {[0, 1, 2, 3, 4].map(i => (
        <span key={i} className="apex-skeleton d-block mb-2" style={{ height: 36 }} />
      ))}
    </div>
  </>
);
