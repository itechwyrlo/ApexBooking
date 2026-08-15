import { useState } from 'react'
import axios from 'axios'
import { Scanner, type IDetectedBarcode } from '@yudiel/react-qr-scanner'
import { Modal } from '../common/Modal'
import { Button } from '../common/Button'
import { useToast } from '../../hooks/useToast'
import { checkoutBooking, getCheckoutDetails, scanArrival } from '../../services/bookingService'
import type { ICheckoutDetails } from '../../interfaces/ICheckoutDetails'

interface IAdmitScanModalProps {
  isOpen: boolean
  onClose: () => void
  onAdmitted: () => void
}

function formatAmount(amount: number, currencyCode: string): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: currencyCode }).format(amount)
}

function extractErrorDetail(error: unknown): string | undefined {
  return axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
}

export function AdmitScanModal({ isOpen, onClose, onAdmitted }: IAdmitScanModalProps) {
  const { showToast } = useToast()
  const [isProcessing, setIsProcessing] = useState(false)
  const [checkoutDetails, setCheckoutDetails] = useState<ICheckoutDetails | null>(null)

  const handleScan = async (results: IDetectedBarcode[]) => {
    const token = results[0]?.rawValue
    if (!token || isProcessing || checkoutDetails) return

    setIsProcessing(true)
    try {
      const result = await scanArrival(token)
      if (result.wasFirstAdmission) {
        showToast('success', `${result.bookingReference} admitted.`)
        onAdmitted()
        onClose()
        return
      }

      // Already admitted, still scheduled — this is the checkout moment.
      const details = await getCheckoutDetails(result.bookingId)
      setCheckoutDetails(details)
    } catch (error) {
      showToast('error', extractErrorDetail(error) ?? 'This boarding pass could not be scanned. Please try again.')
    } finally {
      setIsProcessing(false)
    }
  }

  const handleConfirm = async () => {
    if (!checkoutDetails) return

    setIsProcessing(true)
    try {
      const result = await checkoutBooking(checkoutDetails.bookingId)
      showToast('success', `${result.bookingReference} completed.`)
      onAdmitted()
      handleClose()
    } catch (error) {
      showToast('error', extractErrorDetail(error) ?? 'This checkout could not be confirmed. Please try again.')
    } finally {
      setIsProcessing(false)
    }
  }

  const handleClose = () => {
    setCheckoutDetails(null)
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      title={checkoutDetails ? 'Confirm Checkout' : 'Scan Boarding Pass'}
      onClose={handleClose}
      footer={
        checkoutDetails ? (
          <div className="d-flex justify-content-end gap-2">
            <Button variant="outline-secondary" onClick={handleClose} disabled={isProcessing}>
              Cancel
            </Button>
            <Button onClick={handleConfirm} isLoading={isProcessing}>
              Confirm Checkout
            </Button>
          </div>
        ) : undefined
      }
    >
      {checkoutDetails ? (
        <dl className="row small mb-0 gy-1">
          <dt className="col-4 text-muted fw-normal">Customer</dt>
          <dd className="col-8 mb-0">{checkoutDetails.customerName}</dd>

          <dt className="col-4 text-muted fw-normal">Service</dt>
          <dd className="col-8 mb-0">{checkoutDetails.serviceName}</dd>

          <dt className="col-4 text-muted fw-normal">Team Member</dt>
          <dd className="col-8 mb-0">{checkoutDetails.staffName}</dd>

          <dt className="col-4 text-muted fw-normal">Paid online</dt>
          <dd className="col-8 mb-0">{formatAmount(checkoutDetails.amountPaidOnline, checkoutDetails.currencyCode)}</dd>

          <dt className="col-4 text-muted fw-normal">Left to pay</dt>
          <dd className="col-8 mb-0 fw-semibold">{formatAmount(checkoutDetails.remainingBalance, checkoutDetails.currencyCode)}</dd>
        </dl>
      ) : (
        <>
          <p className="text-muted small">Point the camera at the customer's QR boarding pass to check them in or check them out.</p>
          {isOpen && (
            <div className="rounded overflow-hidden">
              <Scanner onScan={handleScan} paused={isProcessing} formats={['qr_code']} />
            </div>
          )}
        </>
      )}
    </Modal>
  )
}
