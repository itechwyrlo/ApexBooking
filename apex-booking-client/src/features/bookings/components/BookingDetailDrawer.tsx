import React, { useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';
import type { Booking, BookingStatus } from '../types';
import { formatTo12Hour } from '../../../utils/timeFormat';
import { useAuth } from '../../../context/AuthContext';
import './BookingDetailDrawer.styles.css';

interface Props {
  booking: Booking | null;
  onClose: () => void;
  onCancel: (booking: Booking) => void;
  onConfirm: (booking: Booking) => void;
}

const STATUS_LABELS: Record<BookingStatus, string> = {
  PendingPayment: 'Pending Payment',
  Pending: 'Pending',
  Confirmed: 'Confirmed',
  Cancelled: 'Cancelled',
  Completed: 'Completed',
  NoShow: 'No Show',
};

const statusBadgeClass = (status: BookingStatus): string => {
  switch (status) {
    case 'Confirmed':      return 'bg-success-subtle text-success';
    case 'Pending':        return 'bg-warning-subtle text-warning';
    case 'PendingPayment': return 'bg-warning-subtle text-warning';
    case 'Completed':      return 'bg-info-subtle text-info';
    case 'Cancelled':      return 'bg-secondary-subtle text-secondary';
    case 'NoShow':         return 'bg-danger-subtle text-danger';
  }
};

const paymentLabel = (b: Booking): string | null => {
  if (b.status === 'PendingPayment') return 'Pending';
  if (b.priceSnapshot === 0) return 'Free';
  return null;
};

const paymentBadgeClass = (b: Booking): string => {
  if (b.status === 'PendingPayment') return 'bg-warning-subtle text-warning';
  if (b.priceSnapshot === 0) return 'bg-secondary-subtle text-secondary';
  return 'bg-primary-subtle text-primary';
};

const isCancellable = (status: BookingStatus): boolean =>
  status !== 'Cancelled' &&
  status !== 'Completed' &&
  status !== 'NoShow';

const formatDate = (iso: string): string =>
  new Date(iso + 'T00:00:00').toLocaleDateString('en-US', {
    weekday: 'long', year: 'numeric', month: 'long', day: 'numeric',
  });

const BookingDetailDrawer: React.FC<Props> = ({ booking, onClose, onCancel, onConfirm }) => {
  const { user } = useAuth();
  const open = booking !== null;

  useEffect(() => {
    document.body.style.overflow = open ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [open]);

  if (!open || !booking) return null;

  const clientName =
    [booking.guest?.firstName, booking.guest?.lastName].filter(Boolean).join(' ') || 'Unknown Client';

    

  return (
    <>
      <div className="booking-drawer-overlay" onClick={onClose} />

      <div className="booking-drawer">
        <div className="d-flex align-items-start gap-3 p-4 border-bottom">
          <span className="booking-drawer-dot bg-primary mt-1" />
          <div className="flex-grow-1 min-w-0">
            <div className="fw-semibold">{clientName}</div>
            <div className="d-flex gap-2 mt-1 flex-wrap">
              <span className={`booking-status-badge ${statusBadgeClass(booking.status)}`}>
                {STATUS_LABELS[booking.status]}
              </span>
              {paymentLabel(booking) !== null && (
                <span className={`booking-status-badge ${paymentBadgeClass(booking)}`}>
                  {paymentLabel(booking)}
                </span>
              )}
            </div>
          </div>
          <button
            type="button"
            className="booking-drawer-close btn btn-sm btn-light rounded-circle d-flex align-items-center justify-content-center"
            onClick={onClose}
            aria-label="Close"
          >
            <FontAwesomeIcon icon={faXmark} />
          </button>
        </div>

        <div className="flex-grow-1 overflow-auto p-4">
          <div className="text-muted small mb-4">Ref: {booking.bookingReference}</div>

          <div className="d-flex align-items-start gap-2 mb-3">
            <span className="booking-drawer-dot bg-primary mt-1" />
            <div>
              <div className="fw-semibold small">{booking.serviceName}</div>
              <div className="text-muted small">{booking.durationMinutes} min</div>
            </div>
          </div>

          <div className="d-flex align-items-center gap-2 mb-3">
            <i className="fas fa-calendar-alt booking-drawer-icon text-muted" />
            <span className="small">{formatDate(booking.scheduledDate)}</span>
          </div>

          <div className="d-flex align-items-center gap-2 mb-3">
            <i className="fas fa-clock booking-drawer-icon text-muted" />
            <span className="small">
              {formatTo12Hour(booking.scheduledStartTime)} – {formatTo12Hour(booking.scheduledEndTime)}
            </span>
          </div>

          {booking.resourceName && (
            <div className="d-flex align-items-center gap-2 mb-3">
              <i className="fas fa-user booking-drawer-icon text-muted" />
              <span className="small">{booking.resourceName}</span>
            </div>
          )}

          <div className="border-top my-3" />

          {booking.guest?.email && (
            <div className="d-flex align-items-center gap-2 mb-3">
              <i className="fas fa-envelope booking-drawer-icon text-muted" />
              <span className="small">{booking.guest.email}</span>
            </div>
          )}

          {booking.guest?.phone && (
            <div className="d-flex align-items-center gap-2 mb-3">
              <i className="fas fa-phone booking-drawer-icon text-muted" />
              <span className="small">{booking.guest.phone}</span>
            </div>
          )}

          {booking.priceSnapshot > 0 && (
            <div className="d-flex align-items-center gap-2 mb-3">
              <i className="fas fa-tag booking-drawer-icon text-muted" />
              <span className="small">{booking.currencyCode} {Number(booking.priceSnapshot).toFixed(2)}</span>
            </div>
          )}

          {booking.customerNotes && (
            <>
              <div className="border-top my-3" />
              <div className="booking-drawer-section-label">Client Notes</div>
              <p className="small mb-0">{booking.customerNotes}</p>
            </>
          )}

          {booking.cancellationReason && (
            <>
              <div className="border-top my-3" />
              <div className="booking-drawer-section-label">Cancellation Reason</div>
              <p className="small text-danger mb-0">{booking.cancellationReason}</p>
            </>
          )}
        </div>

       

        {(user?.role === 'tenantadmin' || user?.role === 'manager' || user?.role === 'staff' || isCancellable(booking.status)) && (
          <div className="p-4 border-top d-flex flex-column gap-2">
            {(user?.role === 'tenantadmin' || user?.role === 'manager' || user?.role === 'staff') && (
              <button
                type="button"
                className="btn btn-success w-100"
                onClick={() => onConfirm(booking)}
                disabled={booking.status !== 'Pending'}
              >
                Confirm Booking
              </button>
            )}
            {isCancellable(booking.status) && (
              <button
                type="button"
                className="btn btn-outline-danger w-100"
                onClick={() => onCancel(booking)}
              >
                Cancel Booking
              </button>
            )}
          </div>
        )}
      </div>
    </>
  );
};

export default BookingDetailDrawer;
