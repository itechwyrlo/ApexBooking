import { useEffect, useState } from 'react'
import { getServicesByBranch } from '../services/serviceService'
import type { IService } from '../interfaces/IService'

interface IUseServicesByBranchResult {
  services: IService[]
  isLoading: boolean
}

export function useServicesByBranch(branchId: string | null): IUseServicesByBranchResult {
  const [services, setServices] = useState<IService[]>([])
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (!branchId) {
      setServices([])
      return
    }

    let isMounted = true
    setIsLoading(true)

    getServicesByBranch(branchId)
      .then((result) => {
        if (isMounted) setServices(result)
      })
      .catch(() => {
        if (isMounted) setServices([])
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [branchId])

  return { services, isLoading }
}
