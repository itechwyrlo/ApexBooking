import type { MouseEvent, ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { Icon } from './Icon'

type ButtonVariant = 'primary' | 'outline-primary' | 'outline-secondary' | 'danger' | 'outline-danger' | 'light' | 'outline-light' | 'link'
type ButtonSize = 'sm' | 'md' | 'lg'

const SIZE_CLASSES: Record<ButtonSize, string> = {
  sm: 'btn-sm',
  md: '',
  lg: 'btn-lg',
}

interface IButtonProps {
  variant?: ButtonVariant
  size?: ButtonSize
  isLoading?: boolean
  fullWidth?: boolean
  className?: string
  children: ReactNode
  to?: string
  type?: 'button' | 'submit'
  onClick?: (event: MouseEvent<HTMLButtonElement | HTMLAnchorElement>) => void
  disabled?: boolean
  /** Icon name to render before the label (or alone, when `iconOnly` is set). */
  icon?: string
  /**
   * Renders only the icon with the label kept for screen readers and as a
   * native tooltip. Requires `aria-label` so the action stays identifiable.
   */
  iconOnly?: boolean
  'aria-label'?: string
  /** Forwarded via the existing `...rest` spread — for a button that toggles a collapsible region
   * (e.g. CalendarPage's Legend). Purely additive; every existing call site is unaffected. */
  'aria-expanded'?: boolean
}

export function Button({
  variant = 'primary',
  size = 'md',
  isLoading,
  fullWidth,
  className = '',
  children,
  to,
  type = 'button',
  onClick,
  disabled,
  icon,
  iconOnly,
  ...rest
}: IButtonProps) {
  const classes = [
    'btn',
    `btn-${variant}`,
    SIZE_CLASSES[size],
    fullWidth ? 'w-100' : '',
    iconOnly ? 'btn-icon' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  const label = rest['aria-label']
  const iconElement = icon && !isLoading && (
    <Icon name={icon} size={size === 'lg' ? 20 : 16} className={iconOnly ? '' : 'me-2'} />
  )

  const content = (
    <>
      {isLoading && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />}
      {iconElement}
      {iconOnly ? <span className="visually-hidden">{children}</span> : children}
    </>
  )

  if (to !== undefined) {
    return (
      <Link to={to} className={classes} onClick={onClick} title={iconOnly ? label : undefined} {...rest}>
        {content}
      </Link>
    )
  }

  return (
    <button
      type={type}
      className={classes}
      onClick={onClick}
      disabled={isLoading || disabled}
      title={iconOnly ? label : undefined}
      {...rest}
    >
      {content}
    </button>
  )
}
