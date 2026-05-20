import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';

import { Alert } from '../../../components/ui/Alert';
import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
import { Pagination } from '../../../components/ui/pagination/Pagination';
import { Table } from '../../../components/ui/table/table';
import BookingDetailDrawer from '../components/BookingDetailDrawer';
import { BookingsSkeleton } from '../components/BookingsSkeleton';

import { useBookings } from '../hooks/useBookings';
import type { Booking, BookingStatus } from '../types';
import type { Column } from '../../../components/ui/table/types';
import './BookingsPage.styles.css';

const STATUS_LABELS: Record<BookingStatus, string> = {
  PendingPayment: 'Pending Payment',
  Pending: 'Pending',
  Confirmed: 'Confirmed',
  Cancelled: 'Cancelled',
  Completed: 'Completed',
  NoShow: 'No Show',
};

const PAGE_SIZE = 10;

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

const BookingsPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  const { bookings, total, isLoading, error, clearError, getAll, cancel, confirm } = useBookings();

  const [currentPage, setCurrentPage] = useState(1);
  const [selectedBooking, setSelectedBooking] = useState<Booking | null>(null);
  const [showConfirm, setShowConfirm] = useState(false);
  const [targetBooking, setTargetBooking] = useState<Booking | null>(null);
  const [showConfirmApproval, setShowConfirmApproval] = useState(false);
  const [targetConfirmBooking, setTargetConfirmBooking] = useState<Booking | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<BookingStatus | ''>('');

  useEffect(() => {
    getAll(currentPage, PAGE_SIZE);
  }, [getAll, currentPage]);

  const filtered = useMemo(() => {
    return bookings.filter(b => {
      const matchesStatus = !statusFilter || b.status === statusFilter;
      const term = search.toLowerCase();
      const matchesSearch =
        !term ||
        b.bookingReference.toLowerCase().includes(term) ||
        [b.guest?.firstName, b.guest?.lastName].filter(Boolean).join(' ').toLowerCase().includes(term) ||
        (b.guest?.email ?? '').toLowerCase().includes(term);
      return matchesStatus && matchesSearch;
    });
  }, [bookings, search, statusFilter]);

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const openCancel = useCallback((booking: Booking) => {
    setTargetBooking(booking);
    setSelectedBooking(null);
    setShowConfirm(true);
  }, []);

  const handleCancel = async () => {
    if (!targetBooking) return;
    const ok = await cancel(targetBooking.bookingId, async () => {
      await getAll(currentPage, PAGE_SIZE);
    });
    if (ok) {
      setShowConfirm(false);
      setTargetBooking(null);
      setSuccessMessage('Booking cancelled.');
    }
  };

  const openConfirm = useCallback((booking: Booking) => {
    setTargetConfirmBooking(booking);
    setSelectedBooking(null);
    setShowConfirmApproval(true);
  }, []);

  const handleConfirm = async () => {
    if (!targetConfirmBooking) return;
    const ok = await confirm(targetConfirmBooking.bookingId, async () => {
      await getAll(currentPage, PAGE_SIZE);
    });
    if (ok) {
      setShowConfirmApproval(false);
      setTargetConfirmBooking(null);
      setSuccessMessage('Booking confirmed.');
    }
  };

  const columns: Column<Booking>[] = [
    {
      key: 'guest',
      header: 'Client',
      render: (_value, row) => (
        <div>
          <div className="fw-medium small">
            {[row.guest?.firstName, row.guest?.lastName].filter(Boolean).join(' ') || '—'}
          </div>
          <div className="text-muted bookings-sub-text">{row.guest?.email ?? '—'}</div>
        </div>
      ),
    },
    {
      key: 'serviceName',
      header: 'Service',
      render: (value) => (
        <div className="d-flex align-items-center gap-2">
          <span className="bookings-service-dot" />
          <span className="small">{value}</span>
        </div>
      ),
    },
    {
      key: 'resourceName',
      header: 'Staff',
      render: (value) => <span className="text-muted small">{value ?? '—'}</span>,
    },
    {
      key: 'scheduledDate',
      header: 'Date & Time',
      render: (_value, row) => (
        <div>
          <div className="small">{row.scheduledDate}</div>
          <div className="text-muted bookings-sub-text">
            {row.scheduledStartTime.slice(0, 5)} – {row.scheduledEndTime.slice(0, 5)}
          </div>
        </div>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (value) => (
        <span className={`badge bookings-badge ${statusBadgeClass(value as BookingStatus)}`}>
          {STATUS_LABELS[value as BookingStatus]}
        </span>
      ),
    },
    {
      key: 'priceSnapshot',
      header: 'Payment',
      render: (_value, row) => {
        const label = paymentLabel(row);
        if (label === null) return null;
        return (
          <span className={`badge bookings-badge ${paymentBadgeClass(row)}`}>
            {label}
          </span>
        );
      },
    },
  ];

  return (
    <div className="container-fluid px-3 px-md-4 py-4">
      <div className="row mb-4 align-items-center">
        <div className="col">
          <h5 className="fw-bold mb-0">Bookings</h5>
        </div>
        <div className="col-auto">
          <a
            href={`/book/${tenant}/new`}
            target="_blank"
            rel="noreferrer"
            className="btn btn-sm btn-outline-secondary d-flex align-items-center gap-1"
          >
            <i className="fas fa-external-link-alt" />
            Booking Page
          </a>
        </div>
      </div>

      {successMessage && (
        <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-3">
          {successMessage}
        </Alert>
      )}

      {error && (
        <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
          {error}
          <button type="button" className="btn-close ms-auto" onClick={clearError} aria-label="Dismiss" />
        </div>
      )}

      <div className="row mb-3 g-2">
        <div className="col-12 col-md-5">
          <input
            type="text"
            className="form-control form-control-sm"
            placeholder="Search bookings…"
            value={search}
            onChange={e => { setSearch(e.target.value); setCurrentPage(1); }}
          />
        </div>
        <div className="col-auto">
          <select
            className="form-select form-select-sm"
            value={statusFilter}
            onChange={e => { setStatusFilter(e.target.value as BookingStatus | ''); setCurrentPage(1); }}
          >
            <option value="">All statuses</option>
            {(Object.keys(STATUS_LABELS) as BookingStatus[]).map(s => (
              <option key={s} value={s}>{STATUS_LABELS[s]}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {isLoading ? (
            <BookingsSkeleton />
          ) : filtered.length === 0 ? (
            <div className="p-4 text-center text-muted small">No bookings match the current filter.</div>
          ) : (
            <Table
              data={filtered}
              columns={columns}
              getRowId={(b) => b.bookingId}
              onRowClick={(b) => setSelectedBooking(b)}
            />
          )}
        </div>

        {total > PAGE_SIZE && (
          <div className="card-footer bg-white border-top-0">
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              pageSize={PAGE_SIZE}
              totalItems={total}
              onPageChange={setCurrentPage}
            />
          </div>
        )}
      </div>

      <BookingDetailDrawer
        booking={selectedBooking}
        onClose={() => setSelectedBooking(null)}
        onCancel={openCancel}
        onConfirm={openConfirm}
      />

      <ConfirmModal
        isOpen={showConfirm}
        title="Cancel Booking"
        message={`Cancel booking for "${targetBooking?.guest?.firstName ?? 'this client'}"? This cannot be undone.`}
        onConfirm={handleCancel}
        onCancel={() => { setShowConfirm(false); setTargetBooking(null); }}
      />

      <ConfirmModal
        isOpen={showConfirmApproval}
        title="Confirm Booking"
        message={`Confirm booking for "${targetConfirmBooking?.guest?.firstName ?? 'this client'}"?`}
        confirmLabel="Confirm Booking"
        onConfirm={handleConfirm}
        onCancel={() => { setShowConfirmApproval(false); setTargetConfirmBooking(null); }}
      />
    </div>
  );
};

export default BookingsPage;
