import { useEffect, useState, type FormEvent } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import { EwalletFields } from '../refunds/EwalletFields'
import { useToast } from '../../hooks/useToast'
import { usePaymentPolicy } from '../../hooks/usePaymentPolicy'
import { cancelBooking } from '../../services/bookingService'
import { isRequired } from '../../utils/validators'
import { PaymentConfirmationMethod } from '../../types/PaymentConfirmationMethod'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface ICancelBookingModalProps {
  booking: ITenantBooking | null
  onClose: () => void
  onCancelled: () => void
}

export function CancelBookingModal({ booking, onClose, onCancelled }: ICancelBookingModalProps) {
  const { showToast } = useToast()
  const { policy } = usePaymentPolicy()
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [ewalletProvider, setEwalletProvider] = useState('GCash')
  const [ewalletNumber, setEwalletNumber] = useState('')
  const [ewalletName, setEwalletName] = useState('')

  const needsEwallet =
    policy?.refundEnabled === true &&
    booking?.requiresUpfrontPayment === true &&
    booking?.paymentConfirmedVia === PaymentConfirmationMethod.Online

  useEffect(() => {
    if (booking) {
      setReason('')
      setError(null)
      setEwalletProvider('GCash')
      setEwalletNumber('')
      setEwalletName('')
    }
  }, [booking])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!booking) return

    if (!isRequired(reason)) {
      setError('A cancellation reason is required.')
      return
    }

    if (needsEwallet && (!ewalletNumber.trim() || !ewalletName.trim())) {
      setError('E-wallet account number and name are required to cancel a refund-eligible booking.')
      return
    }

    setIsSubmitting(true)
    try {
      await cancelBooking(
        booking.bookingId,
        reason.trim(),
        needsEwallet ? { provider: ewalletProvider, number: ewalletNumber.trim(), name: ewalletName.trim() } : null,
      )
      showToast('success', `Appointment ${booking.bookingReference} was cancelled.`)
      onCancelled()
      onClose()
    } catch (submitError) {
      const detail = axios.isAxiosError(submitError)
        ? (submitError.response?.data as { detail?: string } | undefined)?.detail
        : undefined
      showToast('error', detail ?? 'Failed to cancel this appointment. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal isOpen={booking !== null} title="Cancel Appointment" onClose={onClose}>
      <form noValidate onSubmit={handleSubmit}>
        <p className="text-muted">
          Cancelling <strong>{booking?.bookingReference}</strong> for {booking?.customerName}. This cannot be undone.
        </p>
        <FormGroup label="Reason" htmlFor="cancellationReason" required error={error ?? undefined}>
          <textarea
            id="cancellationReason"
            rows={3}
            className={`form-control ${error ? 'is-invalid' : ''}`}
            value={reason}
            onChange={(e) => {
              setReason(e.target.value)
              setError(null)
            }}
          />
        </FormGroup>
        {needsEwallet && (
          <EwalletFields
            provider={ewalletProvider}
            number={ewalletNumber}
            name={ewalletName}
            disabled={isSubmitting}
            onProviderChange={setEwalletProvider}
            onNumberChange={setEwalletNumber}
            onNameChange={setEwalletName}
          />
        )}
        <div className="modal-form-actions">
          <Button type="button" variant="outline-secondary" onClick={onClose} disabled={isSubmitting}>
            Keep Appointment
          </Button>
          <Button type="submit" variant="danger" isLoading={isSubmitting}>
            {isSubmitting ? 'Cancelling...' : 'Cancel Appointment'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
