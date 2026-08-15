import { useState } from 'react'
import axios from 'axios'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'
import { useToast } from '../../hooks/useToast'
import { useReassignableStaff } from '../../hooks/useReassignableStaff'
import { reassignBooking } from '../../services/bookingService'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface IReassignBookingModalProps {
  isOpen: boolean
  bookings: ITenantBooking[]
  onClose: () => void
  onReassigned: () => void
}

export function ReassignBookingModal({ isOpen, bookings, onClose, onReassigned }: IReassignBookingModalProps) {
  const { showToast } = useToast()
  const [bookingId, setBookingId] = useState('')
  const [newStaffId, setNewStaffId] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const { staff: reassignableStaff, isLoading: isStaffLoading } = useReassignableStaff(bookingId || null)

  const handleClose = () => {
    setBookingId('')
    setNewStaffId('')
    onClose()
  }

  const handleBookingChange = (value: string) => {
    setBookingId(value)
    setNewStaffId('')
  }

  const handleSubmit = async () => {
    if (!bookingId || !newStaffId) return
    setIsSubmitting(true)
    try {
      await reassignBooking(bookingId, newStaffId)
      showToast('success', 'Appointment reassigned.')
      onReassigned()
      handleClose()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to reassign this appointment. Please try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Reassign Barber"
      description="Pick a scheduled appointment, then a new staff member."
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={!bookingId || !newStaffId}>
            Reassign
          </Button>
        </div>
      }
    >
      {bookings.length === 0 ? (
        <p className="text-muted mb-0">No scheduled appointments today.</p>
      ) : (
        <>
          <FormGroup label="Appointment" htmlFor="reassignBooking" required>
            <select
              id="reassignBooking"
              className="form-select"
              value={bookingId}
              onChange={(e) => handleBookingChange(e.target.value)}
              disabled={isSubmitting}
            >
              <option value="">Select an appointment…</option>
              {bookings.map((booking) => (
                <option key={booking.bookingId} value={booking.bookingId}>
                  {booking.customerName} — {booking.serviceName} ({booking.staffName})
                </option>
              ))}
            </select>
          </FormGroup>
          {bookingId && (
            <FormGroup label="New Staff Member" htmlFor="reassignStaff" required>
              <select
                id="reassignStaff"
                className="form-select"
                value={newStaffId}
                onChange={(e) => setNewStaffId(e.target.value)}
                disabled={isSubmitting || isStaffLoading}
              >
                <option value="">{isStaffLoading ? 'Loading…' : 'Select a staff member…'}</option>
                {reassignableStaff.map((member) => (
                  <option key={member.tenantMemberId} value={member.tenantMemberId}>
                    {member.name}
                  </option>
                ))}
              </select>
            </FormGroup>
          )}
        </>
      )}
    </Modal>
  )
}
