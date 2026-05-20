import React from 'react';
import type { DailyRevenueDto } from '../types';

interface WeeklyRevenueChartProps {
  data: DailyRevenueDto[];
}

const formatRevenue = (amount: number): string => {
  if (amount >= 1_000_000) return `₱${(amount / 1_000_000).toFixed(1)}M`;
  if (amount >= 1_000) return `₱${(amount / 1_000).toFixed(1)}K`;
  return `₱${amount}`;
};

export const WeeklyRevenueChart: React.FC<WeeklyRevenueChartProps> = ({ data }) => {
  const maxRevenue = Math.max(...data.map(d => d.revenue), 1);

  return (
    <div className="card border shadow-sm rounded p-3 h-100">
      <div className="fw-bold mb-3" style={{ fontSize: 13, color: '#111827' }}>Weekly Revenue</div>
      {data.length === 0 ? (
        <div className="d-flex align-items-center justify-content-center flex-grow-1 text-muted small">
          No revenue data this week.
        </div>
      ) : (
        <>
          <div className="apex-chart-bars">
            {data.map((d, i) => (
              <div key={i} className="apex-bar-wrap">
                <div className="apex-bar-count">{formatRevenue(d.revenue)}</div>
                <div
                  className="apex-bar"
                  style={{ height: `${Math.max(Math.round((d.revenue / maxRevenue) * 76), 2)}px` }}
                  title={`${d.dayName}: ₱${d.revenue.toLocaleString()}`}
                />
              </div>
            ))}
          </div>
          <div className="d-flex justify-content-around mt-1">
            {data.map((d, i) => (
              <span key={i} className="apex-bar-label">{d.dayName.slice(0, 3)}</span>
            ))}
          </div>
        </>
      )}
    </div>
  );
};
