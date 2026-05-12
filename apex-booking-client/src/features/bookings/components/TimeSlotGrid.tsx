import React from 'react';
import { formatTo12Hour } from '../../../utils/timeFormat';

interface TimeSlotGridProps {
  slots: string[];
  selectedSlot: string | null;
  onSelect: (slot: string) => void;
  isLoading?: boolean;
}

const TimeSlotGrid: React.FC<TimeSlotGridProps> = ({
  slots,
  selectedSlot,
  onSelect,
  isLoading
}) => {
  if (isLoading) {
    return (
      <div className="row g-2">
        {[1, 2, 3, 4, 5, 6].map((i) => (
          <div key={i} className="col-4">
            <div className="placeholder-glow">
              <div className="placeholder col-12 py-3 rounded"></div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (slots.length === 0) {
    return (
      <div className="text-center py-4 text-muted">
        <p className="mb-0 small">No times available on this date. Select a different date.</p>
      </div>
    );
  }

  return (
    <div className="row g-2">
      {slots.map((slot) => (
        <div key={slot} className="col-4">
          <button
            className={`btn btn-sm w-100 py-2 ${selectedSlot === slot ? 'btn-primary' : 'btn-outline-primary'}`}
            onClick={() => onSelect(slot)}
          >
            {formatTo12Hour(slot)}
          </button>
        </div>
      ))}
    </div>
  );
};

export default TimeSlotGrid;
