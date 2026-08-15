import { useCallback, useEffect, useState } from 'react'
import { getTenantRequests } from '../services/superAdminService'
import type { ITenantRequest } from '../interfaces/ITenantRequest'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseTenantRequestsResult {
  requests: ITenantRequest[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useTenantRequests(params: IPageParams = {}): IUseTenantRequestsResult {
  const [requests, setRequests] = useState<ITenantRequest[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getTenantRequests(params)
      .then((result) => {
        if (!isMounted) return
        setRequests(result.data)
        setTotal(result.total)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load tenant requests.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [params.pageNumber, params.pageSize, refreshToken])

  return { requests, total, isLoading, error, refetch }
}
