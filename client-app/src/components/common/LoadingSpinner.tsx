type SpinnerSize = 'sm' | 'md'

interface ILoadingSpinnerProps {
  size?: SpinnerSize
  label?: string
  className?: string
}

export function LoadingSpinner({ size = 'sm', label = 'Loading', className = '' }: ILoadingSpinnerProps) {
  const sizeClass = size === 'sm' ? 'spinner-border-sm' : ''

  return (
    <span className={`d-inline-flex align-items-center gap-2 ${className}`.trim()} role="status">
      <span className={`spinner-border ${sizeClass}`.trim()} aria-hidden="true" />
      <span className="visually-hidden">{label}</span>
    </span>
  )
}
