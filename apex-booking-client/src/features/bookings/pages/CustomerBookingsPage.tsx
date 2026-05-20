import React, { useEffect, useState } from "react";
import { faBan } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { Alert } from "../../../components/ui/Alert";
import { ConfirmModal } from "../../../components/ui/modal/ConfirmModal";
import { useBookings } from "../hooks/useBookings";
import { CustomerBookingsSkeleton } from "../components/CustomerBookingsSkeleton";
import type { Booking, BookingStatus } from "../types";

const BOOKING_STATUS_LABELS: Record<BookingStatus, string> = {
  PendingPayment: "Pending Payment",
  Pending: "Pending",
  Confirmed: "Confirmed",
  Cancelled: "Cancelled",
  Completed: "Completed",
  NoShow: "No Show",
};

const statusBadgeClass = (status: BookingStatus): string => {
  switch (status) {
    case "Confirmed":      return "bg-success-subtle text-success";
    case "Pending":        return "bg-warning-subtle text-warning";
    case "PendingPayment": return "bg-warning-subtle text-warning";
    case "Completed":      return "bg-info-subtle text-info";
    case "Cancelled":      return "bg-secondary-subtle text-secondary";
    case "NoShow":         return "bg-danger-subtle text-danger";
  }
};

const CustomerBookingsPage: React.FC = () => {
  const { bookings, isLoading, error, clearError, getAll, cancel } = useBookings();

  const [showConfirm, setShowConfirm] = useState(false);
  const [targetBooking, setTargetBooking] = useState<Booking | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    getAll();
  }, [getAll]);

  const openCancel = (booking: Booking) => {
    setTargetBooking(booking);
    setShowConfirm(true);
  };

  const handleCancel = async () => {
    if (!targetBooking) return;
    const ok = await cancel(targetBooking.bookingId, getAll);
    if (ok) {
      setShowConfirm(false);
      setTargetBooking(null);
      setSuccessMessage("Booking cancelled.");
    }
  };

  return (
    <div className="container-fluid">
      <div className="mb-4">
        <h4 className="fw-bold mb-1">My Bookings</h4>
        <div className="text-muted small">View and manage your appointments.</div>
      </div>

      {successMessage && (
        <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-3">
          {successMessage}
        </Alert>
      )}

      {error && (
        <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
          {error}
        </Alert>
      )}

      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {isLoading ? (
            <CustomerBookingsSkeleton />
          ) : bookings.length === 0 ? (
            <div className="p-4 text-center text-muted small">
              No bookings found. Book an appointment to get started.
            </div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead className="table-light">
                  <tr>
                    <th className="ps-4 fw-semibold small text-muted">Reference</th>
                    <th className="fw-semibold small text-muted">Service</th>
                    <th className="fw-semibold small text-muted">Staff</th>
                    <th className="fw-semibold small text-muted">Schedule</th>
                    <th className="fw-semibold small text-muted">Status</th>
                    <th className="fw-semibold small text-muted">Price</th>
                    <th className="fw-semibold small text-muted text-end pe-4">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {bookings.map((b) => (
                    <tr key={b.bookingId}>
                      <td className="ps-4">
                        <div className="fw-medium small">{b.bookingReference}</div>
                      </td>
                      <td className="text-muted small">{b.serviceName}</td>
                      <td className="text-muted small">{b.resourceName ?? "—"}</td>
                      <td className="text-muted small">
                        {b.scheduledDate} at {b.scheduledStartTime.slice(0, 5)}
                      </td>
                      <td>
                        <span className={`badge ${statusBadgeClass(b.status)}`}>
                          {BOOKING_STATUS_LABELS[b.status]}
                        </span>
                      </td>
                      <td className="text-muted small">
                        {b.currencyCode} {b.priceSnapshot.toFixed(2)}
                      </td>
                      <td className="text-end pe-4">
                        {b.status !== "Cancelled" && b.status !== "Completed" && b.status !== "NoShow" && (
                          <button
                            className="btn btn-sm btn-outline-danger"
                            title="Cancel"
                            onClick={() => openCancel(b)}
                          >
                            <FontAwesomeIcon icon={faBan} />
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <ConfirmModal
        isOpen={showConfirm}
        title="Cancel Booking"
        message="Cancel this booking? This action cannot be undone."
        onConfirm={handleCancel}
        onCancel={() => { setShowConfirm(false); setTargetBooking(null); }}
      />
    </div>
  );
};

export default CustomerBookingsPage;
