import React, { useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faXmark } from '@fortawesome/free-solid-svg-icons';
import type { ClientSummaryDto, ClientDetailDto, BookingStatus } from '../types';
import { formatTo12Hour } from '../../../utils/timeFormat';
import { useClientDetail } from '../hooks/useClientDetail';
import './ClientDetailDrawer.styles.css';

interface Props {
  client: ClientSummaryDto | null;
  onClose: () => void;
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

const paymentBadgeClass = (status: BookingStatus, price: number): string => {
  if (status === 'PendingPayment') return 'bg-warning-subtle text-warning';
  if (price === 0) return 'bg-secondary-subtle text-secondary';
  return 'bg-primary-subtle text-primary';
};

const paymentLabel = (status: BookingStatus, price: number): string | null => {
  if (status === 'PendingPayment') return 'Pending';
  if (price === 0) return 'Free';
  return null;
};

const formatDate = (iso: string): string =>
  new Date(iso + 'T00:00:00').toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  });

const getInitials = (fullName: string): string => {
  const parts = fullName.trim().split(' ');
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
};

const ClientDetailDrawer: React.FC<Props> = ({ client, onClose }) => {
  const { detail, isLoading, getByEmail, clear } = useClientDetail();
  const open = client !== null;

  useEffect(() => {
    document.body.style.overflow = open ? 'hidden' : '';
    return () => { document.body.style.overflow = ''; };
  }, [open]);

  useEffect(() => {
    if (client) {
      getByEmail(client.email);
    } else {
      clear();
    }
  }, [client, getByEmail, clear]);

  if (!open || !client) return null;

  const loaded: ClientDetailDto | null = detail;

  return (
    <>
      <div className="client-drawer-overlay" onClick={onClose} />

      <div className="client-drawer">
        {/* Header */}
        <div className="d-flex align-items-start gap-3 p-4 border-bottom">
          <div className="client-drawer-avatar">
            {getInitials(client.fullName)}
          </div>
          <div className="flex-grow-1 min-w-0">
            <div className="fw-semibold">{client.fullName}</div>
            <div className="text-muted small">{client.email}</div>
            {client.phone && (
              <div className="text-muted small">{client.phone}</div>
            )}
          </div>
          <button
            type="button"
            className="client-drawer-close btn btn-sm btn-light rounded-circle d-flex align-items-center justify-content-center"
            onClick={onClose}
            aria-label="Close"
          >
            <FontAwesomeIcon icon={faXmark} />
          </button>
        </div>

        {/* Stats */}
        <div className="d-flex gap-2 p-4 border-bottom">
          <div className="client-drawer-stat">
            <div className="client-drawer-stat-value">{client.totalBookings}</div>
            <div className="client-drawer-stat-label">Total Bookings</div>
          </div>
          <div className="client-drawer-stat">
            <div className="client-drawer-stat-value">
              {client.lastVisit ? formatDate(client.lastVisit) : '—'}
            </div>
            <div className="client-drawer-stat-label">Last Visit</div>
          </div>
          <div className="client-drawer-stat">
            <div className="client-drawer-stat-value">
              {client.currencyCode} {Number(client.totalSpent).toFixed(2)}
            </div>
            <div className="client-drawer-stat-label">Total Spent</div>
          </div>
        </div>

        {/* Booking History */}
        <div className="flex-grow-1 overflow-auto p-4">
          <div className="client-drawer-section-label">Booking History</div>

          {isLoading && (
            <div className="text-muted small text-center py-3">Loading...</div>
          )}

          {!isLoading && loaded && loaded.bookings.length === 0 && (
            <div className="text-muted small text-center py-3">No bookings found.</div>
          )}

          {!isLoading && loaded && loaded.bookings.map(b => {
            const pLabel = paymentLabel(b.status, b.priceSnapshot);
            return (
              <div key={b.bookingId} className="client-drawer-booking-row">
                <div className="d-flex align-items-start justify-content-between gap-2 mb-1">
                  <div className="small fw-medium">{b.serviceName}</div>
                  <div className="d-flex gap-1 flex-shrink-0 flex-wrap justify-content-end">
                    <span className={`client-badge badge ${statusBadgeClass(b.status)}`}>
                      {STATUS_LABELS[b.status]}
                    </span>
                    {pLabel !== null && (
                      <span className={`client-badge badge ${paymentBadgeClass(b.status, b.priceSnapshot)}`}>
                        {pLabel}
                      </span>
                    )}
                  </div>
                </div>
                <div className="d-flex align-items-center gap-3">
                  <span className="text-muted small">{b.resourceName || '—'}</span>
                  <span className="text-muted small">
                    {formatDate(b.scheduledDate)} · {formatTo12Hour(b.scheduledStartTime)}–{formatTo12Hour(b.scheduledEndTime)}
                  </span>
                </div>
                {b.priceSnapshot > 0 && (
                  <div className="text-muted small mt-1">
                    {b.currencyCode} {Number(b.priceSnapshot).toFixed(2)}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </>
  );
};

export default ClientDetailDrawer;
