import { useCallback, useEffect, useState } from 'react'
import { getRefundRequests } from '../services/refundRequestService'
import type { IRefundRequest } from '../interfaces/IRefundRequest'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseRefundRequestsResult {
  requests: IRefundRequest[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useRefundRequests(params: IPageParams = {}): IUseRefundRequestsResult {
  const [requests, setRequests] = useState<IRefundRequest[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getRefundRequests(params)
      .then((result) => {
        if (isMounted) {
          setRequests(result.data)
          setTotal(result.total)
        }
      })
      .catch(() => {
        if (isMounted) setError('Failed to load refund requests.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.pageNumber, params.pageSize, refreshToken])

  return { requests, total, isLoading, error, refetch }
}
