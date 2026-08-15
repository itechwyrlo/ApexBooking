interface IIconProps {
  name: string
  size?: number
  className?: string
}

export function Icon({ name, size = 18, className = '' }: IIconProps) {
  return (
    <img
      src={`/assets/icons/${name}.svg`}
      alt=""
      width={size}
      height={size}
      aria-hidden="true"
      className={className}
    />
  )
}
