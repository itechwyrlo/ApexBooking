interface ISkeletonProps {
  width?: string | number
  height?: string | number
  className?: string
  rounded?: boolean
}

export function Skeleton({ width = '100%', height = '1rem', className = '', rounded }: ISkeletonProps) {
  return (
    <span
      className={`skeleton ${rounded ? 'rounded-circle' : ''} ${className}`.trim()}
      style={{ width, height }}
      aria-hidden="true"
    />
  )
}
