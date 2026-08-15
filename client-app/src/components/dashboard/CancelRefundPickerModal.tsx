import { useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface ICancelRefundPickerModalProps {
  isOpen: boolean
  bookings: ITenantBooking[]
  onClose: () => void
  onPicked: (booking: ITenantBooking) => void
}

export function CancelRefundPickerModal({ isOpen, bookings, onClose, onPicked }: ICancelRefundPickerModalProps) {
  const [bookingId, setBookingId] = useState('')

  const handleClose = () => {
    setBookingId('')
    onClose()
  }

  const handleContinue = () => {
    const booking = bookings.find((b) => b.bookingId === bookingId)
    if (!booking) return
    setBookingId('')
    onPicked(booking)
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Cancel & Refund"
      description="Pick a scheduled appointment to cancel."
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose}>
            Close
          </Button>
          <Button onClick={handleContinue} disabled={!bookingId}>
            Continue
          </Button>
        </div>
      }
    >
      {bookings.length === 0 ? (
        <p className="text-muted mb-0">No scheduled appointments today.</p>
      ) : (
        <FormGroup label="Appointment" htmlFor="cancelRefundBooking" required>
          <select
            id="cancelRefundBooking"
            className="form-select"
            value={bookingId}
            onChange={(e) => setBookingId(e.target.value)}
          >
            <option value="">Select an appointment…</option>
            {bookings.map((booking) => (
              <option key={booking.bookingId} value={booking.bookingId}>
                {booking.customerName} — {booking.serviceName}
              </option>
            ))}
          </select>
        </FormGroup>
      )}
    </Modal>
  )
}
