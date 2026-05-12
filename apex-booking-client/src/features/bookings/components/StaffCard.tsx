import React from 'react';
import type { PublicResource } from '../types';

interface Props {
  resource: PublicResource | null;
  isSelected: boolean;
  onSelect: () => void;
  onInfoClick: (() => void) | null;
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

const StaffCard: React.FC<Props> = ({ resource, isSelected, onSelect, onInfoClick }) => {
  const isAnyAvailable = resource === null;

  const cardStyle: React.CSSProperties = {
    borderLeft: isSelected ? '4px solid var(--bs-primary)' : '4px solid transparent',
    background: isSelected ? 'var(--bs-primary-bg-subtle, #cfe2ff)' : '#fff',
    border: isSelected
      ? '1px solid var(--bs-primary-border-subtle, #9ec5fe)'
      : '1px solid #dee2e6',
    borderLeftWidth: 4,
    cursor: 'pointer',
    transition: 'background 0.15s',
  };

  return (
    <div
      className="d-flex align-items-center gap-3 rounded p-3 mb-2"
      style={cardStyle}
      onClick={onSelect}
    >
      <div
        className="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0 fw-bold text-white"
        style={{
          width: 44,
          height: 44,
          background: 'var(--bs-primary)',
          fontSize: 15,
        }}
      >
        {isAnyAvailable
          ? <i className="fas fa-user" style={{ fontSize: 18 }} />
          : getInitials(resource.name)}
      </div>

      <div className="flex-grow-1 min-w-0">
        <div className="fw-semibold" style={{ fontSize: 15 }}>
          {isAnyAvailable ? 'Any Available' : resource.name}
        </div>
        <div className="text-muted small text-truncate">
          {isAnyAvailable
            ? "We'll match you with the best available specialist."
            : resource.description ?? ''}
        </div>
      </div>

      {!isAnyAvailable && onInfoClick && (
        <button
          type="button"
          className="btn btn-sm btn-light rounded-circle d-flex align-items-center justify-content-center flex-shrink-0"
          style={{ width: 30, height: 30, padding: 0 }}
          onClick={e => { e.stopPropagation(); onInfoClick(); }}
          aria-label={`Info about ${resource.name}`}
        >
          <i className="fas fa-info" style={{ fontSize: 11 }} />
        </button>
      )}
    </div>
  );
};

export default StaffCard;
