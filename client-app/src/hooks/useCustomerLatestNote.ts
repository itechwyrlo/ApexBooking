import { useEffect, useState } from 'react'
import { getCustomerLatestNote } from '../services/customerService'
import type { ICustomerLatestNote } from '../interfaces/ICustomerLatestNote'

interface IUseCustomerLatestNoteResult {
  note: ICustomerLatestNote | null
  isLoading: boolean
}

export function useCustomerLatestNote(customerId: string | null): IUseCustomerLatestNoteResult {
  const [note, setNote] = useState<ICustomerLatestNote | null>(null)
  const [isLoading, setIsLoading] = useState(customerId !== null)

  useEffect(() => {
    if (!customerId) {
      setNote(null)
      setIsLoading(false)
      return
    }

    let isMounted = true
    setIsLoading(true)

    getCustomerLatestNote(customerId)
      .then((result) => {
        if (isMounted) setNote(result)
      })
      .catch(() => {
        if (isMounted) setNote(null)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [customerId])

  return { note, isLoading }
}
