import { useMemo } from 'react'
import { useRefundRequests } from './useRefundRequests'
import { RefundRequestStatus } from '../types/RefundRequestStatus'

const WARNING_WINDOW_MS = 2 * 24 * 60 * 60 * 1000

interface IUseRefundReviewReminderResult {
  dueSoonCount: number
  isLoading: boolean
}

export function useRefundReviewReminder(): IUseRefundReviewReminderResult {
  const { requests, isLoading } = useRefundRequests()

  const dueSoonCount = useMemo(() => {
    const now = Date.now()
    return requests.filter(
      (request) => request.status === RefundRequestStatus.PendingReview && new Date(request.dueDate).getTime() - now <= WARNING_WINDOW_MS,
    ).length
  }, [requests])

  return { dueSoonCount, isLoading }
}
