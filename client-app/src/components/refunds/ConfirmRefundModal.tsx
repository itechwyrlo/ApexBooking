import { useState, type ChangeEvent } from 'react'
import { Modal } from '../common/Modal'
import { Button } from '../common/Button'
import { FormGroup } from '../common/FormGroup'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

const MAX_RECEIPT_SIZE_BYTES = 5 * 1024 * 1024
const ALLOWED_RECEIPT_TYPES = ['image/jpeg', 'image/png', 'image/webp']

interface IConfirmRefundModalProps {
  isOpen: boolean
  request: IRefundRequest | null
  isSubmitting: boolean
  onClose: () => void
  onConfirm: (receipt: File) => void
}

export function ConfirmRefundModal({ isOpen, request, isSubmitting, onClose, onConfirm }: IConfirmRefundModalProps) {
  const [receipt, setReceipt] = useState<File | null>(null)
  const [error, setError] = useState<string | null>(null)

  if (!request) return null

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null
    if (!file) {
      setReceipt(null)
      return
    }

    if (!ALLOWED_RECEIPT_TYPES.includes(file.type)) {
      setError('Receipt must be a JPEG, PNG, or WebP image.')
      setReceipt(null)
      return
    }

    if (file.size > MAX_RECEIPT_SIZE_BYTES) {
      setError('Receipt must be 5MB or smaller.')
      setReceipt(null)
      return
    }

    setError(null)
    setReceipt(file)
  }

  const handleClose = () => {
    setReceipt(null)
    setError(null)
    onClose()
  }

  const handleSubmit = () => {
    if (!receipt) return
    onConfirm(receipt)
  }

  return (
    <Modal
      isOpen={isOpen}
      title="Confirm Refund"
      description={`Booking ${request.bookingReference}`}
      onClose={handleClose}
      footer={
        <div className="d-flex justify-content-end gap-2">
          <Button variant="outline-secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} isLoading={isSubmitting} disabled={!receipt}>
            Confirm Refund
          </Button>
        </div>
      }
    >
      <p className="mb-2">
        Confirm you&apos;ve manually sent{' '}
        <strong>
          {request.requestedAmount.toFixed(2)} {request.currencyCode}
        </strong>{' '}
        to the customer, and attach proof of the transfer.
      </p>
      <p className="text-muted small mb-3">
        {request.customerEwalletProvider}: <span className="fw-semibold">{request.customerEwalletNumber}</span>
        {' — '}
        {request.customerEwalletName}
      </p>
      <FormGroup label="Receipt / screenshot" htmlFor="refundReceipt" required error={error ?? undefined}>
        <input
          type="file"
          id="refundReceipt"
          className="form-control"
          accept="image/jpeg,image/png,image/webp"
          onChange={handleFileChange}
        />
      </FormGroup>
    </Modal>
  )
}
