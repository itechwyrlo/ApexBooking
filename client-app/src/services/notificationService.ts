import { authClient } from '../api/clients/authClient'
import type { INotification } from '../interfaces/INotification'

// Raw wire shape pushed over SignalR from ApexBooking.Core.Application.Dtos.NotificationDto —
// distinct field names from the REST NotificationSummary DTO (notificationId, not id), so it needs
// its own mapper rather than being typed directly as INotification.
interface INotificationPushWire {
  notificationId: string
  eventType: string
  title: string
  message: string
  isRead: boolean
  createdAt: string
}

export function toNotificationFromPush(wire: INotificationPushWire): INotification {
  return {
    id: wire.notificationId,
    eventType: wire.eventType,
    title: wire.title,
    message: wire.message,
    isRead: wire.isRead,
    createdAt: wire.createdAt,
  }
}

export async function getLatestNotifications(limit = 20): Promise<INotification[]> {
  const response = await authClient.get<INotification[]>('/api/notifications', { params: { limit } })
  return response.data
}

export async function getUnreadNotificationCount(): Promise<number> {
  const response = await authClient.get<number>('/api/notifications/unread-count')
  return response.data
}

export async function markAllNotificationsRead(): Promise<void> {
  await authClient.post('/api/notifications/mark-all-read')
}
