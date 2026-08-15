import { useEffect, useState } from 'react'
import { getTenantRevenue } from '../services/bookingService'
import type { ITenantRevenue } from '../interfaces/ITenantRevenue'
import type { IDateRange } from '../utils/dateRanges'

interface IUseTenantRevenueResult {
  revenue: ITenantRevenue | null
  isLoading: boolean
}

export function useTenantRevenue({ fromDate, toDate }: IDateRange): IUseTenantRevenueResult {
  const [revenue, setRevenue] = useState<ITenantRevenue | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getTenantRevenue(fromDate, toDate)
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
  }, [fromDate, toDate])

  return { revenue, isLoading }
}
