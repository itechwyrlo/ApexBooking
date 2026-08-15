import { useState } from 'react'
import { Modal } from '../common/Modal'
import { FormGroup } from '../common/FormGroup'
import { Button } from '../common/Button'

interface IRejectRefundModalProps {
  isOpen: boolean
  bookingReference: string
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (reason: string) => void
}

export function RejectRefundModal({ isOpen, bookingReference, isSubmitting, onClose, onSubmit }: IRejectRefundModalProps) {
  const [reason, setReason] = useState('')

  const handleClose = () => {
    setReason('')
    onClose()
  }

  const handleSubmit = () => {
    if (reason.trim().length === 0) return
    onSubmit(reason.trim())
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Reject Refund"
      description={`Booking ${bookingReference}`}
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={reason.trim().length === 0}>
            Reject Refund
          </Button>
        </div>
      }
    >
      <FormGroup label="Reason" htmlFor="rejectReason" required>
        <textarea
          id="rejectReason"
          className="form-control"
          rows={3}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Why is this refund being rejected?"
        />
      </FormGroup>
    </Modal>
  )
}
