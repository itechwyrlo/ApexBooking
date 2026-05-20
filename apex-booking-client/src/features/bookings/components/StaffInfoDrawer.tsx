import React, { useEffect } from 'react';
import type { PublicResource } from '../types';

interface Props {
  resource: PublicResource;
  open: boolean;
  onClose: () => void;
  onSelect: () => void;
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

const StaffInfoDrawer: React.FC<Props> = ({ resource, open, onClose, onSelect }) => {
  useEffect(() => {
    if (open) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => { document.body.style.overflow = ''; };
  }, [open]);

  if (!open) return null;

  return (
    <>
      <div className="staff-drawer-overlay" onClick={onClose} />

      <div className="staff-drawer">
        <div className="d-flex align-items-center gap-3 p-4 border-bottom">
          <div className="staff-drawer-avatar rounded-circle d-flex align-items-center justify-content-center fw-bold text-white">
            {getInitials(resource.name)}
          </div>
          <div className="flex-grow-1 min-w-0">
            <div className="fs-6 fw-semibold">{resource.name}</div>
            {resource.description && (
              <div className="text-muted small text-truncate">{resource.description}</div>
            )}
          </div>
          <button
            type="button"
            className="staff-drawer-close-btn btn btn-sm btn-light rounded-circle d-flex align-items-center justify-content-center"
            onClick={onClose}
            aria-label="Close"
          >
            <i className="fas fa-times" />
          </button>
        </div>

        <div className="flex-grow-1 overflow-auto p-4">
          {resource.availabilitySchedule.length > 0 && (
            <div className="mb-4">
              <p className="staff-drawer-section-title text-uppercase text-muted mb-2">
                Availability
              </p>
              {resource.availabilitySchedule.map(s => (
                <div key={s.dayOfWeek} className="d-flex justify-content-between py-2 border-bottom">
                  <span className="small fw-medium">{s.dayOfWeek}</span>
                  <span className="small text-muted">{s.startTime} – {s.endTime}</span>
                </div>
              ))}
            </div>
          )}

          {resource.servicesOffered.length > 0 && (
            <div>
              <p className="staff-drawer-section-title text-uppercase text-muted mb-2">
                Services Offered
              </p>
              {resource.servicesOffered.map(svc => (
                <div key={svc.serviceId} className="d-flex justify-content-between align-items-start py-2 border-bottom">
                  <div>
                    <div className="small fw-medium">{svc.name}</div>
                    <div className="text-muted small">{svc.durationMinutes} min</div>
                  </div>
                  <div className="small fw-semibold text-primary ms-3 flex-shrink-0">
                    {svc.currencyCode} {Number(svc.price).toFixed(2)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="p-4 border-top">
          <button
            type="button"
            className="btn btn-primary w-100"
            onClick={() => { onSelect(); onClose(); }}
          >
            Book with {resource.name}
          </button>
        </div>
      </div>
    </>
  );
};

export default StaffInfoDrawer;
