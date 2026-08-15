import { useEffect, useState } from 'react'
import { getReassignableStaff } from '../services/bookingService'
import type { IReassignableStaffMember } from '../interfaces/IReassignableStaffMember'

interface IUseReassignableStaffResult {
  staff: IReassignableStaffMember[]
  isLoading: boolean
}

export function useReassignableStaff(bookingId: string | null): IUseReassignableStaffResult {
  const [staff, setStaff] = useState<IReassignableStaffMember[]>([])
  const [isLoading, setIsLoading] = useState(bookingId !== null)

  useEffect(() => {
    if (!bookingId) {
      setStaff([])
      setIsLoading(false)
      return
    }

    let isMounted = true
    setIsLoading(true)

    getReassignableStaff(bookingId)
      .then((result) => {
        if (isMounted) setStaff(result)
      })
      .catch(() => {
        if (isMounted) setStaff([])
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [bookingId])

  return { staff, isLoading }
}
