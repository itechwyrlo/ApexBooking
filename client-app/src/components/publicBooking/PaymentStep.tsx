import { useEffect } from 'react'
import { useBookingPaymentStatus } from '../../hooks/useBookingPaymentStatus'
import { formatMoney } from '../../utils/formatMoney'
import { BookingStatus } from '../../types/BookingStatus'
import type { IBookingInitiationResult } from '../../interfaces/publicBooking/IBookingInitiationResult'
import type { WizardDirection } from '../../hooks/usePublicBookingWizard'

interface IPaymentStepProps {
  result: IBookingInitiationResult
  currencyCode: string
  direction: WizardDirection
  onConfirmed: () => void
}

export function PaymentStep({ result, currencyCode, direction, onConfirmed }: IPaymentStepProps) {
  const { status } = useBookingPaymentStatus(result.ticketToken)

  useEffect(() => {
    if (status && status.status !== BookingStatus.PendingPayment) {
      onConfirmed()
    }
  }, [status, onConfirmed])

  return (
    <div className={`pb-step-enter-${direction} text-center`}>
      <h1 className="pb-display fs-3 mb-1">Complete your payment</h1>
      <p className="pb-muted mb-4">
        Booking reference <span className="pb-mono">{result.bookingReference}</span>
      </p>

      <div className="pb-ticket p-4 mb-4">
        <div className="pb-mono fs-4 fw-semibold mb-3">{formatMoney(result.amountToPay, currencyCode)}</div>

        {result.payMongoQrCodeUrl && (
          <img
            src={result.payMongoQrCodeUrl}
            alt="Scan to pay with GCash or your banking app"
            className="img-fluid rounded-3 mb-3"
            style={{ maxWidth: 220 }}
          />
        )}

        {result.payMongoCheckoutUrl && (
          <a href={result.payMongoCheckoutUrl} target="_blank" rel="noopener noreferrer" className="btn pb-btn-primary w-100">
            Pay Now
          </a>
        )}
      </div>

      <p className="pb-muted small" aria-live="polite">
        <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />
        Waiting for payment confirmation — this page will update automatically once it clears.
      </p>
    </div>
  )
}
