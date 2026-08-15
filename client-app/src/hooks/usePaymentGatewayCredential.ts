import { useCallback, useEffect, useState } from 'react'
import { getPaymentGatewayCredential } from '../services/paymentGatewayService'
import type { IPaymentGatewayCredential } from '../interfaces/IPaymentGatewayCredential'

interface IUsePaymentGatewayCredentialResult {
  credential: IPaymentGatewayCredential | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function usePaymentGatewayCredential(): IUsePaymentGatewayCredentialResult {
  const [credential, setCredential] = useState<IPaymentGatewayCredential | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getPaymentGatewayCredential()
      .then((result) => {
        if (isMounted) setCredential(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load payment gateway settings.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [refreshToken])

  return { credential, isLoading, error, refetch }
}
