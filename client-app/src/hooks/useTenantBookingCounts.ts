import { useCallback, useEffect, useState } from 'react'
import { getTenantBookingCounts } from '../services/bookingService'
import type { ITenantBookingCounts } from '../interfaces/ITenantBookingCounts'

interface IUseTenantBookingCountsResult {
  counts: ITenantBookingCounts | null
  isLoading: boolean
  refetch: () => void
}

export function useTenantBookingCounts(date: string): IUseTenantBookingCountsResult {
  const [counts, setCounts] = useState<ITenantBookingCounts | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getTenantBookingCounts(date)
      .then((result) => {
        if (isMounted) setCounts(result)
      })
      .catch(() => {
        if (isMounted) setCounts(null)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [date, refreshToken])

  return { counts, isLoading, refetch }
}
