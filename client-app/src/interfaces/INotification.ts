// Mirrors ApexBooking.Core.Application.Features.Notifications.Queries.GetLatestNotifications.NotificationSummary
export interface INotification {
  id: string
  eventType: string
  title: string
  message: string
  isRead: boolean
  createdAt: string
}
