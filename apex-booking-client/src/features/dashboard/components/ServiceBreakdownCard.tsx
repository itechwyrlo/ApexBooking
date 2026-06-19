import React from 'react';
import type { ServiceBreakdownDto } from '../types';

interface ServiceBreakdownCardProps {
  data: ServiceBreakdownDto[];
}

const SERVICE_COLORS = ['#0d6efd', '#198754', '#ffc107', '#dc3545', '#6f42c1', '#0dcaf0'];

export const ServiceBreakdownCard: React.FC<ServiceBreakdownCardProps> = ({ data }) => (
  <div className="card border shadow-sm rounded p-3 h-100">
    <div className="fw-bold mb-3" style={{ fontSize: 13, color: '#111827' }}>Services Breakdown</div>
    {data.length === 0 ? (
      <div className="d-flex align-items-center justify-content-center flex-grow-1 text-muted small">
        No service data available.
      </div>
    ) : (
      <div>
        {data.map((item, i) => (
          <div key={item.serviceName} className="mb-3">
            <div className="d-flex justify-content-between align-items-center mb-1">
              <span className="d-flex align-items-center gap-2" style={{ fontSize: 12, color: '#374151' }}>
                <span
                  style={{
                    width: 8,
                    height: 8,
                    borderRadius: '50%',
                    backgroundColor: SERVICE_COLORS[i % SERVICE_COLORS.length],
                    display: 'inline-block',
                    flexShrink: 0,
                  }}
                />
                {item.serviceName}
              </span>
              <span style={{ fontSize: 11, fontWeight: 600, color: '#6b7280' }}>
                {item.percentage}%
              </span>
            </div>
            <div className="apex-service-bar-track">
              <div
                className="apex-service-bar-fill"
                style={{
                  width: `${item.percentage}%`,
                  backgroundColor: SERVICE_COLORS[i % SERVICE_COLORS.length],
                }}
              />
            </div>
          </div>
        ))}
      </div>
    )}
  </div>
);
