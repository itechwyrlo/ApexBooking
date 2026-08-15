import { useCallback, useEffect, useState } from 'react'
import { getPaymentPolicy } from '../services/paymentPolicyService'
import type { IPaymentPolicy } from '../interfaces/IPaymentPolicy'

interface IUsePaymentPolicyResult {
  policy: IPaymentPolicy | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function usePaymentPolicy(): IUsePaymentPolicyResult {
  const [policy, setPolicy] = useState<IPaymentPolicy | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getPaymentPolicy()
      .then((result) => {
        if (isMounted) setPolicy(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load payment settings.')
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
