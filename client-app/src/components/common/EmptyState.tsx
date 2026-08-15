import { Button } from './Button'
import { Icon } from './Icon'

interface IEmptyStateProps {
  title: string
  description: string
  icon?: string
  actionLabel?: string
  onAction?: () => void
}

export function EmptyState({ title, description, icon, actionLabel, onAction }: IEmptyStateProps) {
  return (
    <div className="text-center py-4">
      {icon && (
        <div
          className="d-inline-flex align-items-center justify-content-center rounded-circle mb-3"
          style={{ width: 44, height: 44, backgroundColor: 'var(--color-canvas)' }}
        >
          <Icon name={icon} size={20} />
        </div>
      )}
      <h3 className="fs-6 fw-semibold mb-1">{title}</h3>
      <p className="text-muted mb-3">{description}</p>
      {actionLabel && onAction && (
        <Button variant="primary" size="sm" onClick={onAction}>
          {actionLabel}
        </Button>
      )}
    </div>
  )
}
