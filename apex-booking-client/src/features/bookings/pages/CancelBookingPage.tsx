import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Alert } from '../../../components/ui/Alert';
import { Button } from '../../../components/ui/Button';
import { useCancellationToken } from '../hooks/useCancellationToken';
import type { CancellationTokenValidation } from '../types';

function computeRefundAmount(data: CancellationTokenValidation): number {
  const charged = data.depositOnly
    ? data.depositType === 'Percentage'
      ? (data.priceSnapshot * data.depositValue) / 100
      : data.depositValue
    : data.priceSnapshot;
  return (charged * data.refundPercent) / 100;
}

function formatDate(dateStr: string): string {
  const [y, m, d] = dateStr.split('-').map(Number);
  return new Date(y, m - 1, d).toLocaleDateString('en-US', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

function formatTime(timeStr: string): string {
  const [h, min] = timeStr.split(':').map(Number);
  const d = new Date();
  d.setHours(h, min, 0, 0);
  return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
}

const PageShell: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center py-4">
    <div className="container">
      <div className="row justify-content-center">
        <div className="col-12 col-md-6 col-lg-5">
          <div className="card border-0 shadow-sm p-4">
            {children}
          </div>
        </div>
      </div>
    </div>
  </div>
);

const IconCircle: React.FC<{ color: string; icon: string }> = ({ color, icon }) => (
  <div
    className={`booking-icon-circle rounded-circle bg-${color} bg-opacity-10 d-flex align-items-center justify-content-center mx-auto mb-3`}
  >
    <i className={`${icon} fa-xl text-${color}`} />
  </div>
);

const CancelBookingPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const { state, validationData, isCancelling, cancelError, clearCancelError, cancel } =
    useCancellationToken(token);

  const [confirming, setConfirming] = useState(false);
  const [cancelled, setCancelled] = useState(false);
  const [cancelledRefundAmount, setCancelledRefundAmount] = useState(0);

  const handleCancel = async () => {
    if (!validationData) return;
    const refundAmount = computeRefundAmount(validationData);
    const success = await cancel();
    if (success) {
      setCancelledRefundAmount(refundAmount);
      setCancelled(true);
    }
  };

  if (state === 'loading') {
    return (
      <div className="min-vh-100 bg-light d-flex align-items-center justify-content-center">
        <div className="text-muted small">
          <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true" />
          Verifying your cancellation link...
        </div>
      </div>
    );
  }

  if (cancelled && validationData) {
    const hasRefund = validationData.refundPercent > 0;
    return (
      <PageShell>
        <div className="text-center">
          <IconCircle color="success" icon="fas fa-check" />
          <h5 className="fw-bold mb-2">Your booking has been cancelled</h5>
          <p className="text-muted small mb-0">
            {hasRefund
              ? `Your refund of ${validationData.currencyCode} ${cancelledRefundAmount.toFixed(2)} is being processed.`
              : 'No refund will be issued per the cancellation policy.'}
          </p>
        </div>
      </PageShell>
    );
  }

  if (state === 'invalid') {
    return (
      <PageShell>
        <div className="text-center">
          <IconCircle color="danger" icon="fas fa-exclamation-circle" />
          <h5 className="fw-bold mb-2">Invalid Link</h5>
          <p className="text-muted small mb-0">This link is invalid or has expired.</p>
        </div>
      </PageShell>
    );
  }

  if (state === 'used') {
    return (
      <PageShell>
        <div className="text-center">
          <IconCircle color="secondary" icon="fas fa-ban" />
          <h5 className="fw-bold mb-2">Already Cancelled</h5>
          <p className="text-muted small mb-0">This booking has already been cancelled.</p>
        </div>
      </PageShell>
    );
  }

  if (state === 'expired') {
    return (
      <PageShell>
        <div className="text-center">
          <IconCircle color="warning" icon="fas fa-clock" />
          <h5 className="fw-bold mb-2">Cancellation Window Passed</h5>
          <p className="text-muted small mb-0">
            Your cancellation window has passed. To make changes to your booking, please contact
            the business directly.
          </p>
        </div>
      </PageShell>
    );
  }

  const data = validationData!;
  const refundAmount = computeRefundAmount(data);

  return (
    <PageShell>
      <h5 className="fw-bold mb-1 text-center">Cancel Your Booking</h5>
      <p className="text-muted small text-center mb-4">{data.bookingReference}</p>

      <div className="bg-light rounded p-3 mb-4">
        {(
          [
            ['Service', data.serviceName],
            ['Staff', data.resourceName],
            ['Date', formatDate(data.scheduledDate)],
            ['Time', formatTime(data.scheduledStartTime)],
          ] as [string, string][]
        ).map(([label, value]) => (
          <div key={label} className="d-flex justify-content-between py-2 border-bottom">
            <span className="text-muted small">{label}</span>
            <span className="fw-medium small text-end">{value}</span>
          </div>
        ))}
      </div>

      {state === 'valid_refund' && (
        <Alert variant="info" className="mb-4">
          You will receive a refund of{' '}
          <strong>
            {data.currencyCode} {refundAmount.toFixed(2)}
          </strong>
          .
        </Alert>
      )}

      {state === 'valid_no_refund' && (
        <Alert variant="warning" className="mb-4">
          You will not receive a refund if you cancel.
        </Alert>
      )}

      {cancelError && (
        <Alert variant="error" dismissible onDismiss={clearCancelError} className="mb-3">
          {cancelError}
        </Alert>
      )}

      {state === 'valid_refund' && (
        <Button variant="danger" loading={isCancelling} onClick={handleCancel} className="w-100">
          Cancel My Booking
        </Button>
      )}

      {state === 'valid_no_refund' && !confirming && (
        <Button variant="danger" onClick={() => setConfirming(true)} className="w-100">
          Cancel My Booking
        </Button>
      )}

      {state === 'valid_no_refund' && confirming && (
        <>
          <p className="text-muted small text-center mb-3">
            Are you sure you want to cancel? This cannot be undone and no refund will be issued.
          </p>
          <div className="d-flex gap-2">
            <Button
              variant="danger"
              loading={isCancelling}
              onClick={handleCancel}
              className="flex-grow-1"
            >
              Yes, Cancel
            </Button>
            <Button
              variant="secondary"
              disabled={isCancelling}
              onClick={() => setConfirming(false)}
              className="flex-grow-1"
            >
              No, Go Back
            </Button>
          </div>
        </>
      )}
    </PageShell>
  );
};

export default CancelBookingPage;
