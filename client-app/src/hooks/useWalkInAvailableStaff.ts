import { useCallback, useEffect, useState } from 'react'
import { getWalkInAvailableStaff } from '../services/bookingService'
import type { IWalkInStaffAvailability } from '../interfaces/IWalkInStaffAvailability'

interface IUseWalkInAvailableStaffResult {
  staff: IWalkInStaffAvailability[]
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useWalkInAvailableStaff(branchId: string | null, serviceId: string | null): IUseWalkInAvailableStaffResult {
  const [staff, setStaff] = useState<IWalkInStaffAvailability[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    if (!branchId || !serviceId) {
      setStaff([])
      return
    }

    let isMounted = true
    setIsLoading(true)
    setError(null)

    getWalkInAvailableStaff(branchId, serviceId)
      .then((result) => {
        if (isMounted) setStaff(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load available staff.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [branchId, serviceId, refreshToken])

  return { staff, isLoading, error, refetch }
}
