import { useCallback, useEffect, useState } from 'react'
import { getTenantBookings } from '../services/bookingService'
import type { ITenantBooking, ITenantBookingFilters } from '../interfaces/ITenantBooking'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseTenantBookingsResult {
  bookings: ITenantBooking[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useTenantBookings(filters: ITenantBookingFilters, params: IPageParams = {}): IUseTenantBookingsResult {
  const [bookings, setBookings] = useState<ITenantBooking[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getTenantBookings(filters, params)
      .then((result) => {
        if (!isMounted) return
        setBookings(result.data)
        setTotal(result.total)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load appointments.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [filters.branchId, filters.staffId, filters.status, filters.fromDate, filters.toDate, params.pageNumber, params.pageSize, refreshToken])

  return { bookings, total, isLoading, error, refetch }
}
