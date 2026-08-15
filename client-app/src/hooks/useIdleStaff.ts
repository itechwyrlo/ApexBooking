import { useEffect, useState } from 'react'
import { getIdleStaff } from '../services/teamService'
import type { IIdleStaffMember } from '../interfaces/IIdleStaffMember'

interface IUseIdleStaffResult {
  staff: IIdleStaffMember[]
  isLoading: boolean
}

export function useIdleStaff(): IUseIdleStaffResult {
  const [staff, setStaff] = useState<IIdleStaffMember[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getIdleStaff()
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
  }, [])

  return { staff, isLoading }
}
