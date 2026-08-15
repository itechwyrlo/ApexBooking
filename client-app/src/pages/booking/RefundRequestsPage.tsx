import { useState } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Pagination } from '../../components/common/Pagination'
import { RefundRequestTable } from '../../components/refunds/RefundRequestTable'
import { RejectRefundModal } from '../../components/refunds/RejectRefundModal'
import { ConfirmRefundModal } from '../../components/refunds/ConfirmRefundModal'
import { useRefundRequests } from '../../hooks/useRefundRequests'
import { useToast } from '../../hooks/useToast'
import { confirmRefundRequest, rejectRefundRequest } from '../../services/refundRequestService'
import type { IRefundRequest } from '../../interfaces/IRefundRequest'

const PAGE_SIZE = 10

export function RefundRequestsPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { requests, total, isLoading, refetch } = useRefundRequests({ pageNumber, pageSize: PAGE_SIZE })
  const { showToast } = useToast()
  const [busyId, setBusyId] = useState<string | null>(null)
  const [rejectTarget, setRejectTarget] = useState<IRefundRequest | null>(null)
  const [isRejecting, setIsRejecting] = useState(false)
  const [confirmTarget, setConfirmTarget] = useState<IRefundRequest | null>(null)
  const [isConfirming, setIsConfirming] = useState(false)

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const handleConfirmSubmit = async (receipt: File) => {
    if (!confirmTarget) return
    setIsConfirming(true)
    setBusyId(confirmTarget.id)
    try {
      await confirmRefundRequest(confirmTarget.id, receipt)
      showToast('success', 'Refund confirmed.')
      setConfirmTarget(null)
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to confirm the refund. Please try again.')
    } finally {
      setIsConfirming(false)
      setBusyId(null)
    }
  }

  const handleRejectSubmit = async (reason: string) => {
    if (!rejectTarget) return
    setIsRejecting(true)
    setBusyId(rejectTarget.id)
    try {
      await rejectRefundRequest(rejectTarget.id, reason)
      showToast('success', 'Refund rejected.')
      setRejectTarget(null)
      refetch()
    } catch (error) {
      const detail = axios.isAxiosError(error) ? (error.response?.data as { detail?: string } | undefined)?.detail : undefined
      showToast('error', detail ?? 'Failed to reject the refund. Please try again.')
    } finally {
      setIsRejecting(false)
      setBusyId(null)
    }
  }

  return (
    <div>
      <PageHeader title="Refunds" description="Review and act on refund requests from cancelled bookings." />
      <Card>
        <RefundRequestTable
          requests={requests}
          isLoading={isLoading}
          busyId={busyId}
          onConfirm={setConfirmTarget}
          onReject={setRejectTarget}
        />

        {!isLoading && total > 0 && (
          <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <p className="text-muted small mb-0">
              Page {pageNumber} of {totalPages} ({total} requests)
            </p>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
          </div>
        )}
      </Card>
      {rejectTarget && (
        <RejectRefundModal
          isOpen={rejectTarget !== null}
          bookingReference={rejectTarget.bookingReference}
          isSubmitting={isRejecting}
          onClose={() => setRejectTarget(null)}
          onSubmit={handleRejectSubmit}
        />
      )}
      <ConfirmRefundModal
        isOpen={confirmTarget !== null}
        request={confirmTarget}
        isSubmitting={isConfirming}
        onClose={() => setConfirmTarget(null)}
        onConfirm={handleConfirmSubmit}
      />
    </div>
  )
}
