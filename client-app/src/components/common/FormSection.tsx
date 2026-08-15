import type { ReactNode } from 'react'

interface IFormSectionProps {
  title: string
  description?: string
  children: ReactNode
}

export function FormSection({ title, description, children }: IFormSectionProps) {
  return (
    <fieldset className="mb-4 p-0 border-0">
      <legend className="float-none w-auto p-0 border-0 fs-6 fw-semibold mb-1">{title}</legend>
      {description && <p className="text-muted small mb-3">{description}</p>}
      {children}
    </fieldset>
  )
}
