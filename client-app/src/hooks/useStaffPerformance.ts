import { useEffect, useState } from 'react'
import { getStaffPerformance } from '../services/teamService'
import type { IStaffPerformanceEntry } from '../interfaces/IStaffPerformanceEntry'
import type { IDateRange } from '../utils/dateRanges'

interface IUseStaffPerformanceResult {
  entries: IStaffPerformanceEntry[]
  isLoading: boolean
}

export function useStaffPerformance({ fromDate, toDate }: IDateRange): IUseStaffPerformanceResult {
  const [entries, setEntries] = useState<IStaffPerformanceEntry[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getStaffPerformance(fromDate, toDate)
      .then((result) => {
        if (isMounted) setEntries(result)
      })
      .catch(() => {
        if (isMounted) setEntries([])
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [fromDate, toDate])

  return { entries, isLoading }
}
