import { useCallback, useEffect, useState } from 'react'
import { getServiceStaff } from '../services/serviceService'
import type { IStaffAssignment } from '../interfaces/IStaffAssignment'

interface IUseServiceStaffResult {
  staff: IStaffAssignment[]
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useServiceStaff(serviceId: string | null): IUseServiceStaffResult {
  const [staff, setStaff] = useState<IStaffAssignment[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    if (!serviceId) {
      setStaff([])
      return
    }

    let isMounted = true
    setIsLoading(true)
    setError(null)

    getServiceStaff(serviceId)
      .then((result) => {
        if (isMounted) setStaff(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load staff.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [serviceId, refreshToken])

  return { staff, isLoading, error, refetch }
}
