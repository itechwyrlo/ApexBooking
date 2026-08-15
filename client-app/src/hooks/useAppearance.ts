import { useCallback, useEffect, useState } from 'react'
import { getAppearance } from '../services/appearanceService'
import type { IAppearance } from '../interfaces/IAppearance'

interface IUseAppearanceResult {
  appearance: IAppearance | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useAppearance(): IUseAppearanceResult {
  const [appearance, setAppearance] = useState<IAppearance | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getAppearance()
      .then((result) => {
        if (isMounted) setAppearance(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load appearance settings.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [refreshToken])

  return { appearance, isLoading, error, refetch }
}
