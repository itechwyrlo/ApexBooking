import { useCallback, useEffect, useState } from 'react'
import { getFailedOutboxMessages } from '../services/superAdminService'
import type { IFailedOutboxMessage } from '../interfaces/IFailedOutboxMessage'

interface IUseFailedOutboxMessagesResult {
  messages: IFailedOutboxMessage[]
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useFailedOutboxMessages(): IUseFailedOutboxMessagesResult {
  const [messages, setMessages] = useState<IFailedOutboxMessage[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    getFailedOutboxMessages()
      .then((result) => {
        if (isMounted) setMessages(result)
      })
      .catch(() => {
        if (isMounted) setError('Failed to load failed notifications.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [refreshToken])

  return { messages, isLoading, error, refetch }
}
