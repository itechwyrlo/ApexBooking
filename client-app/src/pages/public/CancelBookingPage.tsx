import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { PublicBookingLayout } from '../../layouts/PublicBookingLayout'
import { Button } from '../../components/common/Button'
import { EwalletFields } from '../../components/refunds/EwalletFields'
import { getCancellableBooking, cancelBookingByToken } from '../../services/publicBookingService'
import { formatDisplayDate, formatDisplayTime } from '../../utils/formatDateTime'
import type { ICancellableBooking } from '../../interfaces/publicBooking/ICancellableBooking'

const UNAVAILABLE_COPY: Record<string, string> = {
  'already-cancelled': 'This booking has already been cancelled.',
  'already-completed': 'This appointment has already been completed and can no longer be cancelled.',
  'already-no-show': 'This appointment was already marked as a no-show.',
  'pending-payment': "This booking is still awaiting payment confirmation and can't be cancelled online yet.",
  'past-cutoff': "This booking can no longer be cancelled online — it's too close to the appointment time. Please contact the business directly.",
}

type PageState = 'loading' | 'error' | 'preview' | 'cancelling' | 'cancelled'

export function CancelBookingPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [state, setState] = useState<PageState>('loading')
  const [booking, setBooking] = useState<ICancellableBooking | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [reason, setReason] = useState('')

  // Collected up front, alongside the cancellation itself, whenever the booking qualifies for a
  // refund — no more separate follow-up step after the fact.
  const [ewalletProvider, setEwalletProvider] = useState('GCash')
  const [ewalletNumber, setEwalletNumber] = useState('')
  const [ewalletName, setEwalletName] = useState('')

  useEffect(() => {
    if (!token) {
      setState('error')
      setErrorMessage('This cancellation link is missing its token.')
      return
    }

    let isMounted = true

    getCancellableBooking(token)
      .then((result) => {
        if (!isMounted) return
        setBooking(result)
        setState('preview')
      })
      .catch((error) => {
        if (!isMounted) return
        const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
        setErrorMessage(detail ?? 'This cancellation link is invalid or has expired.')
        setState('error')
      })

    return () => {
      isMounted = false
    }
  }, [token])

  const needsEwallet = booking?.isRefundEligible === true
  const canSubmit = !needsEwallet || (ewalletNumber.trim().length > 0 && ewalletName.trim().length > 0)

  const handleCancel = async () => {
    if (!canSubmit) return

    setState('cancelling')
    try {
      await cancelBookingByToken(
        token,
        reason.trim() || null,
        needsEwallet ? { provider: ewalletProvider, number: ewalletNumber.trim(), name: ewalletName.trim() } : null,
      )
      setState('cancelled')
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      setErrorMessage(detail ?? 'Failed to cancel this booking. Please try again.')
      setState('preview')
    }
  }

  return (
    <PublicBookingLayout currentIndex={0} total={1} stepLabels={[]} showProgress={false}>
      {state === 'loading' && <p className="pb-muted">Loading your booking…</p>}

      {state === 'error' && (
        <div className="text-center">
          <h1 className="pb-display fs-3 mb-2">This link isn't available</h1>
          <p className="pb-muted">{errorMessage}</p>
        </div>
      )}

      {state === 'cancelled' && (
        <div className="text-center">
          <div className="pb-badge-success mb-4">✓ Cancelled</div>
          <h1 className="pb-display fs-3 mb-2">Your booking has been cancelled</h1>
          {booking && (
            <p className="pb-muted">
              {booking.serviceName} with {booking.staffName} on {formatDisplayDate(booking.scheduledDate)} has been cancelled.
            </p>
          )}
          {needsEwallet && (
            <div className="alert alert-success mt-4" role="alert">
              Your refund is being reviewed — we'll email you once it's decided.
            </div>
          )}
        </div>
      )}

      {(state === 'preview' || state === 'cancelling') && booking && (
        <div>
          <h1 className="pb-display fs-3 mb-3">Cancel your booking?</h1>

          <div className="pb-ticket text-start mb-4">
            <div className="p-4">
              <div className="fw-semibold fs-5 mb-1">{booking.serviceName}</div>
              <div className="pb-muted mb-3">
                with {booking.staffName} · {booking.branchName}
              </div>
              <div className="d-flex justify-content-between pb-mono fs-6 fw-semibold">
                <span>{formatDisplayDate(booking.scheduledDate)}</span>
                <span>{formatDisplayTime(booking.scheduledStartTime)}</span>
              </div>
            </div>
            <div className="pb-ticket-divider mx-4" />
            <div className="p-4">
              <div className="pb-muted small text-uppercase mb-1" style={{ letterSpacing: '0.06em' }}>
                Booking reference
              </div>
              <div className="pb-mono pb-muted fw-semibold">{booking.bookingReference}</div>
            </div>
          </div>

          {!booking.canCancelOnline ? (
            <div className="alert alert-warning" role="alert">
              {UNAVAILABLE_COPY[booking.unavailableReason ?? ''] ?? 'This booking can no longer be cancelled online.'}
            </div>
          ) : (
            <>
              {errorMessage && (
                <div className="alert alert-danger pb-alert-danger" role="alert">
                  {errorMessage}
                </div>
              )}

              <div className="mb-3">
                <label className="form-label small" htmlFor="cancelReason">
                  Reason (optional)
                </label>
                <textarea
                  id="cancelReason"
                  className="form-control"
                  rows={3}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  disabled={state === 'cancelling'}
                />
              </div>

              {needsEwallet && (
                <div className="text-start mb-3">
                  <EwalletFields
                    provider={ewalletProvider}
                    number={ewalletNumber}
                    name={ewalletName}
                    disabled={state === 'cancelling'}
                    onProviderChange={setEwalletProvider}
                    onNumberChange={setEwalletNumber}
                    onNameChange={setEwalletName}
                  />
                </div>
              )}

              <Button variant="danger" fullWidth isLoading={state === 'cancelling'} disabled={!canSubmit} onClick={handleCancel}>
                {state === 'cancelling' ? 'Cancelling…' : 'Cancel My Booking'}
              </Button>
            </>
          )}
        </div>
      )}
    </PublicBookingLayout>
  )
}
