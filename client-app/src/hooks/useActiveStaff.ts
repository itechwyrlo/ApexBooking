import { useEffect, useState } from 'react'
import { getTeamMembers } from '../services/teamService'
import { TenantMemberStatus } from '../types/TenantMemberStatus'
import type { ITeamMember } from '../interfaces/ITeamMember'

interface IUseActiveStaffResult {
  staff: ITeamMember[]
  isLoading: boolean
}

export function useActiveStaff(): IUseActiveStaffResult {
  const [staff, setStaff] = useState<ITeamMember[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getTeamMembers({ pageSize: 1000 })
      .then((result) => {
        if (!isMounted) return
        const activeMembers = result.data
          .filter((member) => member.status === TenantMemberStatus.Active)
          .sort((a, b) => a.firstName.localeCompare(b.firstName))
        setStaff(activeMembers)
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
