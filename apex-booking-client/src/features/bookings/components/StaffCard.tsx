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

  return (
    <div
      className={`staff-card d-flex align-items-center gap-3 rounded p-3 mb-2${isSelected ? ' staff-card--selected' : ''}`}
      onClick={onSelect}
    >
      <div className="staff-card-avatar rounded-circle d-flex align-items-center justify-content-center fw-bold text-white">
        {isAnyAvailable
          ? <i className="fas fa-user" />
          : getInitials(resource.name)}
      </div>

      <div className="flex-grow-1 min-w-0">
        <div className="fw-semibold">
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
          className="staff-card-info-btn btn btn-sm btn-light rounded-circle d-flex align-items-center justify-content-center"
          onClick={e => { e.stopPropagation(); onInfoClick(); }}
          aria-label={`Info about ${resource.name}`}
        >
          <i className="fas fa-info" />
        </button>
      )}
    </div>
  );
};

export default StaffCard;
