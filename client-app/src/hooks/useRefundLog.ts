import { useEffect, useState } from 'react'
import { getRefundLog } from '../services/refundRequestService'
import type { IRefundLogEntry } from '../interfaces/IRefundLogEntry'

interface IUseRefundLogResult {
  entries: IRefundLogEntry[]
  isLoading: boolean
}

export function useRefundLog(): IUseRefundLogResult {
  const [entries, setEntries] = useState<IRefundLogEntry[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)

    getRefundLog()
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
  }, [])

  return { entries, isLoading }
}
