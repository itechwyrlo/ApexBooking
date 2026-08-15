export function formatDisplayDate(isoDate: string): string {
  return new Date(`${isoDate}T00:00:00`).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export function formatDisplayDateTime(isoDateTime: string): string {
  return new Date(isoDateTime).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

export function formatDisplayTime(time: string): string {
  return new Date(`2000-01-01T${time}`).toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  })
}

// e.g. "Just now", "5m ago", "3h ago", "2d ago" — falls back to a short date once it's over a week old.
export function formatRelativeTime(isoDateTime: string): string {
  const then = new Date(isoDateTime).getTime()
  const diffMs = Date.now() - then
  const diffMinutes = Math.floor(diffMs / 60_000)

  if (diffMinutes < 1) return 'Just now'
  if (diffMinutes < 60) return `${diffMinutes}m ago`

  const diffHours = Math.floor(diffMinutes / 60)
  if (diffHours < 24) return `${diffHours}h ago`

  const diffDays = Math.floor(diffHours / 24)
  if (diffDays < 7) return `${diffDays}d ago`

  return new Date(isoDateTime).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}
