import React, { useEffect, useMemo, useState } from 'react';
// import { useParams } from 'react-router-dom';
import { faPlus, faEdit, faBan } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

import { Button } from '../../../components/ui/Button';
import { Alert } from '../../../components/ui/Alert';
import { FormModal } from '../../../components/ui/modal/FormModal';
import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
import { Pagination } from '../../../components/ui/pagination/Pagination';

import { useBookings } from '../hooks/useBookings';
import { useServices } from '../../service/hooks/useServices';
import { useResources } from '../../resources/hooks/useResources';

import type {
  Booking,
  CreateBookingRequest,
  UpdateBookingRequest,
  BookingStatus,
} from '../types';

import type { ModelSchema } from '../../../components/ui/table/types';

const BOOKING_STATUS_LABELS: Record<BookingStatus, string> = {
  Pending: 'Pending',
  Confirmed: 'Confirmed',
  Cancelled: 'Cancelled',
  Completed: 'Completed',
};

const EMPTY_FORM: CreateBookingRequest = {
  serviceId: '',
  resourceId: '',
  scheduledDate: '',
  scheduledStartTime: '',
  customerNotes: '',
};

const PAGE_SIZE = 10;

const BookingsPage: React.FC = () => {
  // const { tenant } = useParams<{ tenant: string }>();

  const { bookings, isLoading, error, clearError, getAll, create, update, cancel } =
    useBookings();

  const { services, getAll: getServices } = useServices();
  const { resources, getAll: getResources } = useResources();

  const [currentPage, setCurrentPage] = useState(1);

  const [showForm, setShowForm] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);

  const [editingBooking, setEditingBooking] = useState<Booking | null>(null);
  const [targetBooking, setTargetBooking] = useState<Booking | null>(null);

  const [formValue, setFormValue] =
    useState<CreateBookingRequest>(EMPTY_FORM);

  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    getAll();
    getServices();
    getResources();
  }, [getAll, getServices, getResources]);

  const filteredResources = useMemo(() => {
    if (!formValue.serviceId) return [];
    const selectedService = services.find(s => s.id === formValue.serviceId);
    if (!selectedService) return [];
    return resources.filter(r => selectedService.resourceIds.includes(r.id));
  }, [services, resources, formValue.serviceId]);

  const bookingFormSchema = useMemo(
    (): ModelSchema<CreateBookingRequest>[] => [
      {
        key: 'serviceId',
        label: 'Service',
        type: 'select',
        required: true,
        dataSource: {
          mode: 'static',
          options: services.map((s) => ({
            label: s.name,
            value: s.id,
          })),
        },
      },
      {
        key: 'resourceId',
        label: 'Resource',
        type: 'select',
        required: true,
        dataSource: {
          mode: 'static',
          options: filteredResources.map((r) => ({
            label: r.name,
            value: r.id,
          })),
        },
      },
      { key: 'scheduledDate', label: 'Scheduled Date', type: 'date', required: true },
      { key: 'scheduledStartTime', label: 'Scheduled Start Time', type: 'time', required: true },
      { key: 'customerNotes', label: 'Customer Notes', type: 'textarea' },
    ],
    [services, filteredResources]
  );

  const handleFormChange = (value: CreateBookingRequest) => {
    if (value.serviceId !== formValue.serviceId) {
      setFormValue({ ...value, resourceId: '' });
    } else {
      setFormValue(value);
    }
  };

  const totalPages = Math.max(1, Math.ceil(bookings.length / PAGE_SIZE));
  const paged = bookings.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const openCreate = () => {
    setEditingBooking(null);
    setFormValue(EMPTY_FORM);
    setShowForm(true);
  };

  const openEdit = (booking: Booking) => {
    setEditingBooking(booking);

    // Extract date (YYYY-MM-DD) and time (HH:mm) from ISO string
    const date = new Date(booking.startTime);
    const scheduledDate = date.toISOString().split('T')[0];
    const scheduledStartTime = date.toTimeString().slice(0, 5);

    setFormValue({
      serviceId: booking.serviceId,
      resourceId: booking.resourceId,
      scheduledDate: scheduledDate,
      scheduledStartTime: scheduledStartTime,
      customerNotes: booking.notes ?? '',
    });

    setShowForm(true);
  };

  const openCancel = (booking: Booking) => {
    setTargetBooking(booking);
    setShowConfirm(true);
  };

  const handleSubmit = async (value: CreateBookingRequest): Promise<void> => {
    if (editingBooking) {
      const req: UpdateBookingRequest = {
        serviceId: value.serviceId,
        resourceId: value.resourceId,
        startTime: value.scheduledStartTime,
        endTime: value.scheduledStartTime,
        notes: value.customerNotes,
      };

      const ok = await update(editingBooking.id, req);

      if (ok) {
        setShowForm(false);
        setSuccessMessage('Booking updated.');
      }

      return;
    }

    const ok = await create(value);

    if (ok) {
      setShowForm(false);
      setSuccessMessage('Booking created.');
    }
  };

  const handleCancel = async () => {
    if (!targetBooking) return;

    const ok = await cancel(targetBooking.id);

    if (ok) {
      setShowConfirm(false);
      setTargetBooking(null);
      setSuccessMessage('Booking cancelled.');
    }
  };

  const getStatusBadgeClass = (status: BookingStatus) => {
    switch (status) {
      case 'Confirmed':
        return 'bg-success-subtle text-success';
      case 'Pending':
        return 'bg-warning-subtle text-warning';
      case 'Completed':
        return 'bg-info-subtle text-info';
      case 'Cancelled':
        return 'bg-secondary-subtle text-secondary';
      default:
        return 'bg-secondary-subtle text-secondary';
    }
  };

  return (
    <div className="container-fluid">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h4 className="fw-bold mb-1">Bookings</h4>
          <div className="text-muted small">Manage scheduled appointments.</div>
        </div>

        <Button variant="primary" icon={faPlus} onClick={openCreate}>
          New Booking
        </Button>
      </div>

      {successMessage && (
        <Alert
          variant="success"
          dismissible
          onDismiss={() => setSuccessMessage(null)}
          className="mb-3"
        >
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
            <div className="p-4 text-center text-muted small">Loading bookings...</div>
          ) : bookings.length === 0 ? (
            <div className="p-4 text-center text-muted small">
              No bookings found. Please try creating a new booking or check back later.
            </div>
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead className="table-light">
                  <tr>
                    <th className="ps-4 fw-semibold small text-muted">Customer</th>
                    <th className="fw-semibold small text-muted">Service</th>
                    <th className="fw-semibold small text-muted">Resource</th>
                    <th className="fw-semibold small text-muted">Schedule</th>
                    <th className="fw-semibold small text-muted">Status</th>
                    <th className="fw-semibold small text-muted text-end pe-4">Actions</th>
                  </tr>
                </thead>

                <tbody>
                  {paged.map((b) => (
                    <tr key={b.id}>
                      <td className="ps-4">
                        <div className="fw-medium">{b.customerName}</div>
                        <div className="text-muted small">{b.customerEmail}</div>
                      </td>

                      <td className="text-muted small">{b.serviceName}</td>
                      <td className="text-muted small">{b.resourceName}</td>

                      <td className="text-muted small">
                        {b.startTime} → {b.endTime}
                      </td>

                      <td>
                        <span className={`badge ${getStatusBadgeClass(b.status)}`}>
                          {BOOKING_STATUS_LABELS[b.status]}
                        </span>
                      </td>

                      <td className="text-end pe-4">
                        <div className="d-flex justify-content-end gap-2">
                          <button
                            className="btn btn-sm btn-outline-primary"
                            title="Edit"
                            onClick={() => openEdit(b)}
                          >
                            <FontAwesomeIcon icon={faEdit} />
                          </button>

                          {b.status !== 'Cancelled' && (
                            <button
                              className="btn btn-sm btn-outline-danger"
                              title="Cancel"
                              onClick={() => openCancel(b)}
                            >
                              <FontAwesomeIcon icon={faBan} />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {bookings.length > PAGE_SIZE && (
          <div className="card-footer bg-white border-top-0">
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              pageSize={PAGE_SIZE}
              totalItems={bookings.length}
              onPageChange={setCurrentPage}
              onPageSizeChange={() => {}}
            />
          </div>
        )}
      </div>

      <FormModal
        isOpen={showForm}
        title={editingBooking ? 'Edit Booking' : 'New Booking'}
        fields={bookingFormSchema}
        value={formValue}
        onChange={handleFormChange}
        onSubmit={handleSubmit}
        onClose={() => setShowForm(false)}
      />

      <ConfirmModal
        isOpen={showConfirm}
        title="Cancel Booking"
        message={`Cancel booking for "${targetBooking?.customerName}"? This action cannot be undone.`}
        onConfirm={handleCancel}
        onCancel={() => {
          setShowConfirm(false);
          setTargetBooking(null);
        }}
      />
    </div>
  );
};

export default BookingsPage;