import { useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface ISaveChairNotesModalProps {
  isOpen: boolean
  bookings: ITenantBooking[]
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (bookingId: string, notes: string) => void
}

export function SaveChairNotesModal({ isOpen, bookings, isSubmitting, onClose, onSubmit }: ISaveChairNotesModalProps) {
  const [bookingId, setBookingId] = useState('')
  const [notes, setNotes] = useState('')

  const handleClose = () => {
    setBookingId('')
    setNotes('')
    onClose()
  }

  const handleSubmit = () => {
    if (!bookingId || notes.trim().length === 0) return
    onSubmit(bookingId, notes.trim())
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Save Chair Notes"
      description="Log details for a client you just finished with."
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={!bookingId || notes.trim().length === 0}>
            Save Notes
          </Button>
        </div>
      }
    >
      {bookings.length === 0 ? (
        <p className="text-muted mb-0">You haven&apos;t completed any appointments today yet.</p>
      ) : (
        <>
          <FormGroup label="Appointment" htmlFor="chairNotesBooking" required>
            <select
              id="chairNotesBooking"
              className="form-select"
              value={bookingId}
              onChange={(e) => setBookingId(e.target.value)}
              disabled={isSubmitting}
            >
              <option value="">Select a completed appointment…</option>
              {bookings.map((booking) => (
                <option key={booking.bookingId} value={booking.bookingId}>
                  {booking.customerName} — {booking.serviceName}
                </option>
              ))}
            </select>
          </FormGroup>
          <FormGroup label="Notes" htmlFor="chairNotesText" required>
            <textarea
              id="chairNotesText"
              className="form-control"
              rows={3}
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="e.g. Prefers scissors over clippers, sensitive scalp…"
              disabled={isSubmitting}
            />
          </FormGroup>
        </>
      )}
    </Modal>
  )
}
