import { useCallback, useEffect, useState } from 'react'
import { getBusinessProfile } from '../services/businessProfileService'
import type { IBusinessProfile } from '../interfaces/IBusinessProfile'

interface IUseBusinessProfileResult {
  profile: IBusinessProfile | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useBusinessProfile(): IUseBusinessProfileResult {
  const [profile, setProfile] = useState<IBusinessProfile | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getBusinessProfile()
      .then((result) => {
        if (isMounted) setProfile(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load business profile.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [refreshToken])

  return { profile, isLoading, error, refetch }
}
