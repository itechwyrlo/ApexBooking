import React, { useCallback, useEffect, useMemo, useState } from 'react';

import { Alert } from '../../../components/ui/Alert';
import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
import BookingDetailDrawer from '../components/BookingDetailDrawer';
import BookingEventCalendar from '../components/BookingEventCalendar';
import { CalendarSkeleton } from '../components/CalendarSkeleton';
import { useBookings } from '../hooks/useBookings';
import { useCalendarBookings } from '../hooks/useCalendarBookings';
import type { Booking, BookingStatus } from '../types';

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

const formatDetailDate = (dateStr: string): string =>
  new Date(dateStr + 'T00:00:00').toLocaleDateString('en-US', {
    weekday: 'long', year: 'numeric', month: 'long', day: 'numeric',
  });

const CalendarPage: React.FC = () => {
  const today = new Date();

  const { bookings, isLoading, error, clearError, fetchMonth } = useCalendarBookings();
  const { cancel, confirm } = useBookings();

  const [currentYear, setCurrentYear] = useState(today.getFullYear());
  const [currentMonth, setCurrentMonth] = useState(today.getMonth() + 1);
  const [selectedDate, setSelectedDate] = useState<string | null>(null);
  const [selectedBooking, setSelectedBooking] = useState<Booking | null>(null);
  const [showConfirm, setShowConfirm] = useState(false);
  const [targetBooking, setTargetBooking] = useState<Booking | null>(null);
  const [showConfirmApproval, setShowConfirmApproval] = useState(false);
  const [targetConfirmBooking, setTargetConfirmBooking] = useState<Booking | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [hasLoaded, setHasLoaded] = useState(false);

  useEffect(() => {
    fetchMonth(currentYear, currentMonth).then(() => setHasLoaded(true));
  }, [fetchMonth, currentYear, currentMonth]);

  const handleMonthChange = useCallback((year: number, month: number) => {
    setCurrentYear(year);
    setCurrentMonth(month);
  }, []);

  const selectedDateBookings = useMemo(() => {
    if (!selectedDate) return [];
    return bookings
      .filter(b => b.scheduledDate === selectedDate)
      .slice()
      .sort((a, b) => a.scheduledStartTime.localeCompare(b.scheduledStartTime));
  }, [bookings, selectedDate]);

  const openCancel = useCallback((booking: Booking) => {
    setTargetBooking(booking);
    setSelectedBooking(null);
    setShowConfirm(true);
  }, []);

  const handleCancel = async () => {
    if (!targetBooking) return;
    const ok = await cancel(targetBooking.bookingId, async () => {
      await fetchMonth(currentYear, currentMonth);
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
      await fetchMonth(currentYear, currentMonth);
    });
    if (ok) {
      setShowConfirmApproval(false);
      setTargetConfirmBooking(null);
      setSuccessMessage('Booking confirmed.');
    }
  };

  return (
    <div className="container-fluid px-3 px-md-4 py-4">
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

      <div className="card border-0 shadow-sm">
        <div className="card-body p-3 p-md-4">
          {!hasLoaded && isLoading ? (
            <CalendarSkeleton />
          ) : (
            <BookingEventCalendar
              bookings={bookings}
              isLoading={isLoading}
              onMonthChange={handleMonthChange}
              onEventClick={setSelectedBooking}
              onDateSelect={setSelectedDate}
            />
          )}

          {selectedDate && (
            <div className="apex-cal-detail mt-3">
              <div className="apex-cal-detail-title">
                {formatDetailDate(selectedDate)}
              </div>

              {selectedDateBookings.length === 0 ? (
                <p className="text-muted small mb-0">No bookings for this day.</p>
              ) : (
                selectedDateBookings.map(b => (
                  <div
                    key={b.bookingId}
                    className="apex-cal-detail-item"
                    onClick={() => setSelectedBooking(b)}
                  >
                    <span className="apex-cal-time">
                      {b.scheduledStartTime.slice(0, 5)}
                    </span>
                    <div className="flex-grow-1 min-w-0">
                      <div className="apex-cal-detail-name">
                        {[b.guest?.firstName, b.guest?.lastName].filter(Boolean).join(' ') || 'Unknown Client'}
                      </div>
                      <div className="apex-cal-detail-meta">{b.serviceName}</div>
                    </div>
                    <span className={`badge ${statusBadgeClass(b.status)}`} style={{ fontSize: '10px' }}>
                      {STATUS_LABELS[b.status]}
                    </span>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
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

export default CalendarPage;
