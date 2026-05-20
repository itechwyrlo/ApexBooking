export interface NotificationDto {
  notificationId: string;
  eventType: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationSummaryDto {
  items: NotificationDto[];
  unreadCount: number;
}
