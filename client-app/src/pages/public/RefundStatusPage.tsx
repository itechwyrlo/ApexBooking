import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { PublicBookingLayout } from '../../layouts/PublicBookingLayout'
import { getRefundStatus } from '../../services/refundStatusService'
import { RefundRequestStatus } from '../../types/RefundRequestStatus'
import type { IRefundStatus } from '../../interfaces/IRefundStatus'

type PageState = 'loading' | 'error' | 'ready'

const STATUS_COPY: Partial<Record<RefundRequestStatus, string>> = {
  [RefundRequestStatus.PendingReview]: 'Your refund is awaiting review.',
  [RefundRequestStatus.Refunded]: 'Your refund has been sent.',
  [RefundRequestStatus.Rejected]: 'This refund request was not approved.',
}

export function RefundStatusPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [state, setState] = useState<PageState>('loading')
  const [status, setStatus] = useState<IRefundStatus | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  useEffect(() => {
    if (!token) {
      setState('error')
      setErrorMessage('This refund status link is missing its token.')
      return
    }

    let isMounted = true

    getRefundStatus(token)
      .then((result) => {
        if (!isMounted) return
        setStatus(result)
        setState('ready')
      })
      .catch((error) => {
        if (!isMounted) return
        const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
        setErrorMessage(detail ?? 'This refund status link is invalid or has expired.')
        setState('error')
      })

    return () => {
      isMounted = false
    }
  }, [token])

  return (
    <PublicBookingLayout currentIndex={0} total={1} stepLabels={[]} showProgress={false}>
      {state === 'loading' && <p className="pb-muted">Loading your refund status…</p>}

      {state === 'error' && (
        <div className="text-center">
          <h1 className="pb-display fs-3 mb-2">This link isn't available</h1>
          <p className="pb-muted">{errorMessage}</p>
        </div>
      )}

      {status && state === 'ready' && (
        <div>
          <h1 className="pb-display fs-3 mb-3">Refund Status</h1>

          <div className="pb-ticket text-start mb-4">
            <div className="p-4">
              <div className="pb-muted small text-uppercase mb-1" style={{ letterSpacing: '0.06em' }}>
                Booking reference
              </div>
              <div className="pb-mono fw-semibold mb-3">{status.bookingReference}</div>

              {status.status === null ? (
                <p className="mb-0">There's no refund associated with this booking.</p>
              ) : (
                <>
                  <p className="mb-0">{STATUS_COPY[status.status]}</p>
                  {status.amount !== null && (
                    <p className="fw-semibold fs-5 mt-2 mb-0">
                      {status.amount.toFixed(2)} {status.currencyCode}
                    </p>
                  )}
                  {status.receiptUrl && (
                    <a href={status.receiptUrl} target="_blank" rel="noreferrer" className="d-inline-block mt-2">
                      View refund receipt
                    </a>
                  )}
                </>
              )}
            </div>
          </div>

          {status.businessContactPhoneNumber && (
            <p className="pb-muted small mt-4 text-center">
              Questions? Contact the business at {status.businessContactPhoneNumber}.
            </p>
          )}
        </div>
      )}
    </PublicBookingLayout>
  )
}
