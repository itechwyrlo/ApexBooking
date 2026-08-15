import { useCallback, useEffect, useState } from 'react'
import { getServices } from '../services/serviceService'
import type { IService } from '../interfaces/IService'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseServicesResult {
  services: IService[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useServices(params: IPageParams = {}): IUseServicesResult {
  const [services, setServices] = useState<IService[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getServices(params)
      .then((result) => {
        if (!isMounted) return
        setServices(result.data)
        setTotal(result.total)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load services.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [params.pageNumber, params.pageSize, refreshToken])

  return { services, total, isLoading, error, refetch }
}
