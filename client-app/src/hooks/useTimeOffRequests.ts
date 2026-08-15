import { useCallback, useEffect, useState } from 'react'
import { getTimeOffRequests } from '../services/timeOffService'
import type { ITimeOffFilters, ITimeOffRequest } from '../interfaces/ITimeOffRequest'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseTimeOffRequestsResult {
  requests: ITimeOffRequest[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useTimeOffRequests(filters: ITimeOffFilters, params: IPageParams = {}): IUseTimeOffRequestsResult {
  const [requests, setRequests] = useState<ITimeOffRequest[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getTimeOffRequests(filters, params)
      .then((result) => {
        if (!isMounted) return
        setRequests(result.data)
        setTotal(result.total)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load time-off requests.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [filters.status, params.pageNumber, params.pageSize, refreshToken])

  return { requests, total, isLoading, error, refetch }
}
