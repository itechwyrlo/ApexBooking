import { useEffect, useState } from 'react'
import { getBookingStatus } from '../services/publicBookingService'
import { BookingStatus } from '../types/BookingStatus'
import type { IPublicBookingStatus } from '../interfaces/IPublicBookingStatus'

interface IUseBookingPaymentStatusResult {
  status: IPublicBookingStatus | null
  isLoading: boolean
  error: string | null
}

export function useBookingPaymentStatus(ticketToken: string, intervalMs = 4000): IUseBookingPaymentStatusResult {
  const [status, setStatus] = useState<IPublicBookingStatus | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isMounted = true
    let timeoutId: ReturnType<typeof setTimeout> | undefined

    const poll = () => {
      getBookingStatus(ticketToken)
        .then((result) => {
          if (!isMounted) return
          setStatus(result)
          setError(null)

          // Only keep polling while payment is still outstanding — a recursive setTimeout (rather
          // than setInterval) so a slow request can never overlap with the next tick.
          if (result.status === BookingStatus.PendingPayment) {
            timeoutId = setTimeout(poll, intervalMs)
          }
        })
        .catch(() => {
          if (isMounted) setError('Failed to check booking status.')
        })
        .finally(() => {
          if (isMounted) setIsLoading(false)
        })
    }

    poll()

    return () => {
      isMounted = false
      if (timeoutId) clearTimeout(timeoutId)
    }
  }, [ticketToken, intervalMs])

  return { status, isLoading, error }
}
