import { useCallback, useEffect, useState } from 'react'
import { getStaffBreaks } from '../services/teamService'
import type { IStaffBreak } from '../interfaces/IStaffBreak'

interface IUseStaffBreaksResult {
  breaks: IStaffBreak[]
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useStaffBreaks(tenantMemberId: string | null): IUseStaffBreaksResult {
  const [breaks, setBreaks] = useState<IStaffBreak[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    if (!tenantMemberId) {
      setBreaks([])
      return
    }

    let isMounted = true
    setIsLoading(true)
    setError(null)

    getStaffBreaks(tenantMemberId)
      .then((result) => {
        if (isMounted) setBreaks(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load breaks.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [tenantMemberId, refreshToken])

  return { breaks, isLoading, error, refetch }
}
