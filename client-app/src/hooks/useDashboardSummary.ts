import { useCallback, useEffect, useState } from 'react'
import { getDashboardSummary } from '../services/superAdminService'
import type { IPlatformDashboardSummary } from '../interfaces/IPlatformDashboardSummary'

interface IUseDashboardSummaryResult {
  summary: IPlatformDashboardSummary | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useDashboardSummary(): IUseDashboardSummaryResult {
  const [summary, setSummary] = useState<IPlatformDashboardSummary | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getDashboardSummary()
      .then((result) => {
        if (isMounted) setSummary(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load platform summary.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [refreshToken])

  return { summary, isLoading, error, refetch }
}
