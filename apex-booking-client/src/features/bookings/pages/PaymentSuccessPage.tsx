import React, { useEffect, useState } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import axiosInstance from '../../../services/axiosInstance';
import type { PublicBookingDto } from '../types';

const PaymentSuccessPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const bookingId = searchParams.get('bookingId');

  const [booking, setBooking] = useState<PublicBookingDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!bookingId) {
      setError('Booking reference not found.');
      setIsLoading(false);
      return;
    }

    const load = async () => {
      try {
        const result = await axiosInstance.get<PublicBookingDto>(
          `/public/${tenant}/bookings/${bookingId}`
        );
        setBooking(result ?? null);
      } catch {
        setError('Failed to load booking.');
      } finally {
        setIsLoading(false);
      }
    };

    load();
  }, [bookingId, tenant]);

  if (isLoading) {
    return (
      <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center">
        <div className="text-muted small">Confirming your payment...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center py-4">
        <div className="container">
          <div className="row justify-content-center">
            <div className="col-12 col-md-6">
              <div className="card border-0 shadow-sm p-5 text-center">
                <div className="text-danger small mb-3">{error}</div>
                <button
                  className="btn btn-outline-primary btn-sm"
                  onClick={() => navigate(`/book/${tenant}`)}
                >
                  Return to Booking Page
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center py-4">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-12 col-md-6">
            <div className="card border-0 shadow-sm p-5 text-center">
              <div className="booking-icon-circle rounded-circle bg-success bg-opacity-10 d-flex align-items-center justify-content-center mx-auto mb-3">
                <i className="fas fa-check fa-xl text-success" />
              </div>
              <h5 className="fw-bold mb-2">Payment Successful</h5>
              <p className="text-muted small mb-3">
                Your payment has been received and your booking is confirmed.
              </p>
              {booking && (
                <>
                  <div className="bg-light rounded p-3 mb-3">
                    <div className="text-muted small mb-1">Booking Reference</div>
                    <div className="fw-bold fs-5">{booking.bookingReference}</div>
                  </div>
                  <div className="text-muted small mb-4">
                    <div>{booking.scheduledDate} at {booking.scheduledStartTime}</div>
                  </div>
                </>
              )}
              <button
                className="btn btn-outline-primary btn-sm"
                onClick={() => navigate(`/book/${tenant}/new`)}
              >
                Book Another Appointment
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default PaymentSuccessPage;
