import React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core';

interface StatCardProps {
  label: string;
  value: string;
  icon: IconDefinition;
  iconBg: string;
  iconColor: string;
  sub: string;
}

export const StatCard: React.FC<StatCardProps> = ({ label, value, icon, iconBg, iconColor, sub }) => (
  <div className="card border shadow-sm rounded p-3 h-100">
    <div className="d-flex justify-content-between align-items-start mb-2">
      <div className="apex-stat-label">{label}</div>
      <div className="apex-stat-icon" style={{ backgroundColor: iconBg }}>
        <FontAwesomeIcon icon={icon} style={{ color: iconColor }} />
      </div>
    </div>
    <div className="apex-stat-value mb-1">{value}</div>
    <div className="text-muted" style={{ fontSize: 12 }}>{sub}</div>
  </div>
);
