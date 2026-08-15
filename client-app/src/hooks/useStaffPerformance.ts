import { useEffect, useState } from 'react'
import { getStaffPerformance } from '../services/teamService'
import type { IStaffPerformanceEntry } from '../interfaces/IStaffPerformanceEntry'

interface IUseStaffPerformanceResult {
  entries: IStaffPerformanceEntry[]
  isLoading: boolean
}

export function useStaffPerformance(date: string): IUseStaffPerformanceResult {
  const [entries, setEntries] = useState<IStaffPerformanceEntry[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getStaffPerformance(date)
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
  }, [date])

  return { entries, isLoading }
}
