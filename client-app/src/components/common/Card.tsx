import type { ReactNode } from 'react'

interface ICardProps {
  className?: string
  bodyClassName?: string
  hover?: boolean
  children: ReactNode
}

export function Card({ className = '', bodyClassName = '', hover, children }: ICardProps) {
  const cardClasses = ['card', 'border-0', 'shadow-sm', hover ? 'card-hover' : '', className].filter(Boolean).join(' ')
  const bodyClasses = ['card-body', bodyClassName].filter(Boolean).join(' ')

  return (
    <div className={cardClasses}>
      <div className={bodyClasses}>{children}</div>
    </div>
  )
}
