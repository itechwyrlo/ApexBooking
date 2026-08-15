import { useEffect, useState } from 'react'
import { getTenantRevenue } from '../services/bookingService'
import type { ITenantRevenue } from '../interfaces/ITenantRevenue'

interface IUseTenantRevenueResult {
  revenue: ITenantRevenue | null
  isLoading: boolean
}

export function useTenantRevenue(date: string): IUseTenantRevenueResult {
  const [revenue, setRevenue] = useState<ITenantRevenue | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getTenantRevenue(date)
      .then((result) => {
        if (isMounted) setRevenue(result)
      })
      .catch(() => {
        if (isMounted) setRevenue(null)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [date])

  return { revenue, isLoading }
}
