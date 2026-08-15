import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RowActions } from '../common/RowActions'
import { ActiveStatusBadge } from '../common/ActiveStatusBadge'
import { formatMoney } from '../../utils/formatMoney'
import type { IService } from '../../interfaces/IService'

const COLUMNS = ['Service', 'Duration', 'Price', 'Buffer', 'Status', '']

interface IServiceTableProps {
  services: IService[]
  isLoading?: boolean
  onEdit: (service: IService) => void
}

function formatBuffer(service: IService): string {
  return service.bufferBeforeMinutes > 0 || service.bufferAfterMinutes > 0
    ? `${service.bufferBeforeMinutes}m before / ${service.bufferAfterMinutes}m after`
    : '—'
}

export function ServiceTable({ services, isLoading, onEdit }: IServiceTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (services.length === 0) {
    return (
      <EmptyState
        icon="services"
        title="No services set up yet"
        description="Add the services customers can book, like haircuts, checkups, or repairs."
      />
    )
  }

  return (
    <div className="table-responsive">
      <table className="table table-refined table-stack align-middle mb-0">
        <thead>
          <tr className="text-muted small text-uppercase">
            {COLUMNS.map((column) => (
              <th key={column} scope="col" className={`fw-semibold ${['Duration', 'Price', 'Buffer'].includes(column) ? 'text-end' : ''}`}>
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {services.map((service) => (
            <tr key={service.id}>
              <td data-label="Service">
                <div className="fw-semibold">{service.name}</div>
                {service.description && <div className="text-muted small">{service.description}</div>}
              </td>
              <td className="text-end" data-label="Duration">
                {service.durationMinutes} min
              </td>
              <td className="text-end" data-label="Price">
                {formatMoney(service.price, service.currencyCode)}
              </td>
              <td className="text-end" data-label="Buffer">
                {formatBuffer(service)}
              </td>
              <td data-label="Status">
                <ActiveStatusBadge isActive={service.isActive} />
              </td>
              <td className="text-end" data-label="Actions">
                <RowActions actions={[{ label: `Edit ${service.name}`, icon: 'edit', tone: 'edit', onClick: () => onEdit(service) }]} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
