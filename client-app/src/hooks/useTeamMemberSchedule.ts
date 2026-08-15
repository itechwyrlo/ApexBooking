import { useCallback, useEffect, useState } from 'react'
import { getTeamMemberSchedule } from '../services/teamService'
import type { IDaySchedule } from '../interfaces/IDaySchedule'

interface IUseTeamMemberScheduleResult {
  schedule: IDaySchedule[]
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useTeamMemberSchedule(tenantMemberId: string | null): IUseTeamMemberScheduleResult {
  const [schedule, setSchedule] = useState<IDaySchedule[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    if (!tenantMemberId) {
      setSchedule([])
      return
    }

    let isMounted = true
    setIsLoading(true)
    setError(null)

    getTeamMemberSchedule(tenantMemberId)
      .then((result) => {
        if (isMounted) setSchedule(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load working hours.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [tenantMemberId, refreshToken])

  return { schedule, isLoading, error, refetch }
}
