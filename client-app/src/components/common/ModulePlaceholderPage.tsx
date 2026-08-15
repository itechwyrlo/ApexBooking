import { EmptyState } from './EmptyState'
import { PageHeader } from './PageHeader'

interface IModulePlaceholderPageProps {
  title: string
  description: string
  emptyTitle?: string
  icon?: string
}

export function ModulePlaceholderPage({
  title,
  description,
  emptyTitle = 'Nothing here yet',
  icon,
}: IModulePlaceholderPageProps) {
  return (
    <div>
      <PageHeader title={title} />
      <EmptyState icon={icon} title={emptyTitle} description={description} />
    </div>
  )
}
