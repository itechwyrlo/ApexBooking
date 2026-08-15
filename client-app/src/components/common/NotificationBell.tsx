import { useNotifications } from '../../hooks/useNotifications'
import { formatRelativeTime } from '../../utils/formatDateTime'
import { Icon } from './Icon'

export function NotificationBell() {
  const { notifications, unreadCount, isLoading, error, markAllRead } = useNotifications()

  return (
    <div className="dropdown">
      <button
        type="button"
        className="btn btn-light position-relative border-0 d-inline-flex align-items-center justify-content-center"
        style={{ width: 40, height: 40 }}
        data-bs-toggle="dropdown"
        aria-expanded="false"
        aria-label={unreadCount > 0 ? `Notifications, ${unreadCount} unread` : 'Notifications'}
      >
        <Icon name="bell" size={18} />
        {unreadCount > 0 && (
          <span
            className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
            style={{ fontSize: '0.65rem' }}
          >
            {unreadCount > 9 ? '9+' : unreadCount}
            <span className="visually-hidden">unread notifications</span>
          </span>
        )}
      </button>

      <div className="dropdown-menu dropdown-menu-end shadow p-0" style={{ width: 340, maxWidth: '90vw' }}>
        <div className="d-flex align-items-center justify-content-between px-3 py-2 border-bottom">
          <span className="fw-semibold">Notifications</span>
          {unreadCount > 0 && (
            <button type="button" className="btn btn-link btn-sm p-0 text-decoration-none" onClick={() => void markAllRead()}>
              Mark all as read
            </button>
          )}
        </div>

        <div style={{ maxHeight: 360, overflowY: 'auto' }}>
          {isLoading && (
            <div className="px-3 py-4 text-center text-muted small">Loading notifications&hellip;</div>
          )}

          {!isLoading && error && <div className="px-3 py-4 text-center text-danger small">{error}</div>}

          {!isLoading && !error && notifications.length === 0 && (
            <div className="px-3 py-4 text-center text-muted small">
              You&rsquo;re all caught up. New notifications will appear here.
            </div>
          )}

          {!isLoading &&
            !error &&
            notifications.map((notification) => (
              <div
                key={notification.id}
                className={`px-3 py-2 border-bottom d-flex gap-2 ${notification.isRead ? '' : 'bg-light'}`}
              >
                <span
                  className={`rounded-circle flex-shrink-0 ${notification.isRead ? 'bg-transparent' : 'bg-primary'}`}
                  style={{ width: 8, height: 8, marginTop: 6 }}
                  aria-hidden="true"
                />
                <div className="flex-grow-1" style={{ minWidth: 0 }}>
                  <div className="fw-semibold small text-truncate">{notification.title}</div>
                  <div className="text-muted small text-break">{notification.message}</div>
                  <div className="text-muted" style={{ fontSize: '0.75rem' }}>
                    {formatRelativeTime(notification.createdAt)}
                  </div>
                </div>
              </div>
            ))}
        </div>
      </div>
    </div>
  )
}
