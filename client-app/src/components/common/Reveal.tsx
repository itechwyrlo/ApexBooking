import type { ReactNode } from 'react'
import { useRevealOnScroll } from '../../hooks/useRevealOnScroll'

interface IRevealProps {
  children: ReactNode
  className?: string
  delayStep?: number
}

export function Reveal({ children, className = '', delayStep = 0 }: IRevealProps) {
  const { ref, isVisible } = useRevealOnScroll<HTMLDivElement>()

  const style = delayStep ? { transitionDelay: `${Math.min(delayStep, 6) * 90}ms` } : undefined

  return (
    <div ref={ref} className={`reveal ${isVisible ? 'reveal--visible' : ''} ${className}`.trim()} style={style}>
      {children}
    </div>
  )
}
