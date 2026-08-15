import { useCallback, useEffect, useState } from 'react'
import { getBookingPolicy } from '../services/bookingPolicyService'
import type { IBookingPolicy } from '../interfaces/IBookingPolicy'

interface IUseBookingPolicyResult {
  policy: IBookingPolicy | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useBookingPolicy(): IUseBookingPolicyResult {
  const [policy, setPolicy] = useState<IBookingPolicy | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getBookingPolicy()
      .then((result) => {
        if (isMounted) setPolicy(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load booking settings.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [refreshToken])

  return { policy, isLoading, error, refetch }
}
