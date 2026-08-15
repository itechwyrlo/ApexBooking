import { useCallback, useEffect, useRef, useState } from 'react'
import { createNotificationHubConnection } from '../api/clients/notificationHubConnection'
import {
  getLatestNotifications,
  getUnreadNotificationCount,
  markAllNotificationsRead,
  toNotificationFromPush,
} from '../services/notificationService'
import type { INotification } from '../interfaces/INotification'

interface IUseNotificationsResult {
  notifications: INotification[]
  unreadCount: number
  isLoading: boolean
  error: string | null
  refetch: () => void
  markAllRead: () => Promise<void>
}

export function useNotifications(): IUseNotificationsResult {
  const [notifications, setNotifications] = useState<INotification[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [refreshToken, setRefreshToken] = useState(0)
  const connectionRef = useRef<ReturnType<typeof createNotificationHubConnection> | null>(null)

  const refetch = useCallback(() => setRefreshToken((token) => token + 1), [])

  useEffect(() => {
    let isMounted = true
    setIsLoading(true)
    setError(null)

    Promise.all([getLatestNotifications(), getUnreadNotificationCount()])
      .then(([latest, count]) => {
        if (isMounted) {
          setNotifications(latest)
          setUnreadCount(count)
        }
      })
      .catch(() => {
        if (isMounted) setError('Failed to load notifications.')
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [refreshToken])

  // Live push — this is purely a latency optimization on top of the fetch above, not the source of
  // truth. If the connection never establishes (offline, server briefly down), the bell still works
  // via the REST fetch; it just won't update until the user next opens the dropdown.
  useEffect(() => {
    const connection = createNotificationHubConnection()
    connectionRef.current = connection
    // React StrictMode intentionally mounts every effect twice in development (mount ->
    // cleanup -> mount again), which cancels this first connection mid-negotiation — that
    // rejection isn't a real failure, just our own cleanup racing the in-flight start().
    let stoppedByCleanup = false

    connection.on('ReceiveNotification', (wire: Parameters<typeof toNotificationFromPush>[0]) => {
      const incoming = toNotificationFromPush(wire)
      setNotifications((current) => [incoming, ...current])
      setUnreadCount((count) => count + 1)
    })

    connection.onreconnecting((err) => console.warn('[notifications] SignalR reconnecting:', err))
    connection.onreconnected(() => console.info('[notifications] SignalR reconnected'))
    connection.onclose((err) => console.warn('[notifications] SignalR connection closed:', err))

    connection
      .start()
      .then(() => console.info('[notifications] SignalR connected'))
      .catch((err) => {
        if (stoppedByCleanup) return
        // Non-fatal — see comment above, the bell still works via the REST fetch — but this must
        // be visible somewhere, or a silent auth/CORS/URL failure looks identical to "it works,
        // just slow," which is exactly what's impossible to tell apart from the UI alone.
        console.error('[notifications] SignalR connection failed:', err)
      })

    return () => {
      stoppedByCleanup = true
      connectionRef.current = null
      void connection.stop()
    }
  }, [])

  const markAllRead = useCallback(async () => {
    await markAllNotificationsRead()
    setNotifications((current) => current.map((n) => ({ ...n, isRead: true })))
    setUnreadCount(0)
  }, [])

  return { notifications, unreadCount, isLoading, error, refetch, markAllRead }
}
