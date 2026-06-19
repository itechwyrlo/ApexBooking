import React from 'react';
import { toRelativeTime } from '../../../utils/timeFormat';
import type { NotificationDto } from '../types';
import './NotificationItem.styles.css';

interface NotificationItemProps {
  notification: NotificationDto;
}

export const NotificationItem: React.FC<NotificationItemProps> = ({ notification }) => {
  return (
    <div className={`px-3 py-2 border-bottom${notification.isRead ? '' : ' bg-light'}`}>
      <div className="d-flex align-items-start justify-content-between gap-2">
        <div className="flex-grow-1 overflow-hidden">
          <div className="small fw-semibold text-dark text-truncate">{notification.title}</div>
          <div className="small text-muted">{notification.message}</div>
        </div>
        {!notification.isRead && (
          <span className="notif-unread-dot bg-primary mt-1" />
        )}
      </div>
      <div className="small text-muted mt-1">{toRelativeTime(notification.createdAt)}</div>
    </div>
  );
};
