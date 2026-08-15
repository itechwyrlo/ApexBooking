import { useCallback, useEffect, useState } from 'react'
import { getBranches } from '../services/branchService'
import type { IBranch } from '../interfaces/IBranch'

interface IUseBranchesResult {
  branches: IBranch[]
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useBranches(): IUseBranchesResult {
  const [branches, setBranches] = useState<IBranch[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getBranches()
      .then((result) => {
        if (isMounted) setBranches(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load branches.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [refreshToken])

  return { branches, isLoading, error, refetch }
}
