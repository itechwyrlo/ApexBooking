import { useCallback, useEffect, useState } from 'react'
import { getTeamMembers } from '../services/teamService'
import type { ITeamMember } from '../interfaces/ITeamMember'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseTeamMembersResult {
  members: ITeamMember[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useTeamMembers(params: IPageParams = {}): IUseTeamMembersResult {
  const [members, setMembers] = useState<ITeamMember[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getTeamMembers(params)
      .then((result) => {
        if (!isMounted) return
        setMembers(result.data)
        setTotal(result.total)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load team members.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [params.pageNumber, params.pageSize, refreshToken])

  return { members, total, isLoading, error, refetch }
}
