import { Link } from 'react-router-dom'

export interface IBreadcrumbItem {
  label: string
  href?: string
}

interface IBreadcrumbProps {
  items: IBreadcrumbItem[]
}

export function Breadcrumb({ items }: IBreadcrumbProps) {
  if (items.length === 0) return null

  return (
    <nav aria-label="Breadcrumb" className="mb-1">
      <ol className="breadcrumb mb-0 small">
        {items.map((item, index) => {
          const isLast = index === items.length - 1

          return (
            <li key={item.label} className={`breadcrumb-item${isLast ? ' active' : ''}`} aria-current={isLast ? 'page' : undefined}>
              {item.href && !isLast ? <Link to={item.href}>{item.label}</Link> : item.label}
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
