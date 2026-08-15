import type { ReactNode } from 'react'
import { Breadcrumb, type IBreadcrumbItem } from './Breadcrumb'

interface IPageHeaderProps {
  title: string
  description?: string
  breadcrumbs?: IBreadcrumbItem[]
  primaryAction?: ReactNode
  secondaryAction?: ReactNode
}

export function PageHeader({ title, description, breadcrumbs, primaryAction, secondaryAction }: IPageHeaderProps) {
  return (
    <div className="d-flex flex-column flex-md-row align-items-md-center justify-content-between gap-3 mb-4">
      <div>
        {breadcrumbs && breadcrumbs.length > 0 && <Breadcrumb items={breadcrumbs} />}
        <h1 className="fs-3 fw-bold mb-1">{title}</h1>
        {description && <p className="text-muted mb-0">{description}</p>}
      </div>
      {(primaryAction || secondaryAction) && (
        <div className="d-flex flex-wrap gap-2">
          {secondaryAction}
          {primaryAction}
        </div>
      )}
    </div>
  )
}
