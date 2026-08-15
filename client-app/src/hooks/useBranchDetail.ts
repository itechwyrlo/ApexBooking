import { useCallback, useEffect, useState } from 'react'
import { getBranch } from '../services/branchService'
import type { IBranchDetail } from '../interfaces/IBranchDetail'

interface IUseBranchDetailResult {
  branch: IBranchDetail | null
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useBranchDetail(branchId: string | null): IUseBranchDetailResult {
  const [branch, setBranch] = useState<IBranchDetail | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    if (!branchId) {
      setBranch(null)
      return
    }

    let isMounted = true
    setIsLoading(true)
    setError(null)

    getBranch(branchId)
      .then((result) => {
        if (isMounted) setBranch(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load branch.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [branchId, refreshToken])

  return { branch, isLoading, error, refetch }
}
