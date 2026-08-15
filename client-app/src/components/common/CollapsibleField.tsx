import type { ReactNode } from 'react'

interface ICollapsibleFieldProps {
  isOpen: boolean
  children: ReactNode
}

export function CollapsibleField({ isOpen, children }: ICollapsibleFieldProps) {
  return (
    <div className={`collapsible-field ${isOpen ? 'is-open' : ''}`.trim()}>
      <div className="collapsible-field-inner">{children}</div>
    </div>
  )
}
