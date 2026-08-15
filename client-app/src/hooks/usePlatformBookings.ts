import { useCallback, useEffect, useState } from 'react'
import { getPlatformBookings } from '../services/superAdminService'
import type { IPlatformBooking, IPlatformBookingFilters } from '../interfaces/IPlatformBooking'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUsePlatformBookingsResult {
  bookings: IPlatformBooking[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function usePlatformBookings(
  filters: IPlatformBookingFilters,
  params: IPageParams = {},
): IUsePlatformBookingsResult {
  const [bookings, setBookings] = useState<IPlatformBooking[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getPlatformBookings(filters, params)
      .then((result) => {
        if (!isMounted) return
        setBookings(result.data)
        setTotal(result.total)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load bookings.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [filters.tenantId, filters.status, filters.fromDate, filters.toDate, params.pageNumber, params.pageSize, refreshToken])

  return { bookings, total, isLoading, error, refetch }
}
