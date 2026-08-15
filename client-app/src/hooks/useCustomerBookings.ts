import { useCallback, useEffect, useState } from 'react'
import { getCustomerBookings } from '../services/customerService'
import type { ICustomerBooking } from '../interfaces/ICustomerBooking'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseCustomerBookingsResult {
  bookings: ICustomerBooking[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useCustomerBookings(customerId: string | null, params: IPageParams = {}): IUseCustomerBookingsResult {
  const [bookings, setBookings] = useState<ICustomerBooking[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    if (!customerId) {
      setBookings([])
      setTotal(0)
      return
    }

    let isMounted = true
    setIsLoading(true)
    setError(null)

    getCustomerBookings(customerId, params)
      .then((result) => {
        if (!isMounted) return
        setBookings(result.data)
        setTotal(result.total)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load booking history.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [customerId, params.pageNumber, params.pageSize, refreshToken])

  return { bookings, total, isLoading, error, refetch }
}
