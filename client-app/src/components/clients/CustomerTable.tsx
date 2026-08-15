import { EmptyState } from '../common/EmptyState'
import { TableSkeleton } from '../common/TableSkeleton'
import { RowActions } from '../common/RowActions'
import type { ICustomer } from '../../interfaces/ICustomer'

const COLUMNS = ['Client', 'Contact', 'Client Since', '']

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

interface ICustomerTableProps {
  customers: ICustomer[]
  isLoading?: boolean
  onViewBookings: (customer: ICustomer) => void
}

export function CustomerTable({ customers, isLoading, onViewBookings }: ICustomerTableProps) {
  if (isLoading) {
    return <TableSkeleton columns={COLUMNS.length} rows={5} />
  }

  if (customers.length === 0) {
    return (
      <EmptyState
        icon="clients"
        title="No clients yet"
        description="Clients will appear here once they book an appointment with your business."
      />
    )
  }

  return (
    <div className="table-responsive">
      <table className="table table-stack align-middle mb-0">
        <thead>
          <tr className="text-muted small text-uppercase">
            {COLUMNS.map((column) => (
              <th key={column} scope="col" className="fw-semibold">
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {customers.map((customer) => (
            <tr key={customer.id}>
              <td className="fw-semibold" data-label="Client">
                {customer.name}
              </td>
              <td data-label="Contact">
                {customer.email && <div>{customer.email}</div>}
                {customer.phoneNumber && <div className="text-muted small">{customer.phoneNumber}</div>}
                {!customer.email && !customer.phoneNumber && <span className="text-muted">—</span>}
              </td>
              <td data-label="Client Since">{formatDate(customer.createdAt)}</td>
              <td className="text-end" data-label="Actions">
                <RowActions
                  actions={[{ label: `View bookings for ${customer.name}`, icon: 'eye', tone: 'view', onClick: () => onViewBookings(customer) }]}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
