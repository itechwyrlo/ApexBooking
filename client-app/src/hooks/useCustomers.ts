import { useCallback, useEffect, useState } from 'react'
import { getCustomers } from '../services/customerService'
import type { ICustomer } from '../interfaces/ICustomer'
import type { IPageParams } from '../interfaces/IPagedResult'

interface IUseCustomersResult {
  customers: ICustomer[]
  total: number
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useCustomers(params: IPageParams = {}): IUseCustomersResult {
  const [customers, setCustomers] = useState<ICustomer[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getCustomers(params)
      .then((result) => {
        if (!isMounted) return
        setCustomers(result.data)
        setTotal(result.total)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load clients.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [params.pageNumber, params.pageSize, refreshToken])

  return { customers, total, isLoading, error, refetch }
}
