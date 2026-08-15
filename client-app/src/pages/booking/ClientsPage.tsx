import { useState } from 'react'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Pagination } from '../../components/common/Pagination'
import { CustomerTable } from '../../components/clients/CustomerTable'
import { CustomerBookingsModal } from '../../components/clients/CustomerBookingsModal'
import { useCustomers } from '../../hooks/useCustomers'
import type { ICustomer } from '../../interfaces/ICustomer'

const PAGE_SIZE = 10

export function ClientsPage() {
  const [pageNumber, setPageNumber] = useState(1)
  const { customers, total, isLoading } = useCustomers({ pageNumber, pageSize: PAGE_SIZE })
  const [selectedCustomer, setSelectedCustomer] = useState<ICustomer | null>(null)

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div>
      <PageHeader
        title="Clients"
        description="Browse the customers who have booked with your business and review their booking history."
      />
      <Card>
        <CustomerTable customers={customers} isLoading={isLoading} onViewBookings={setSelectedCustomer} />

        {!isLoading && total > 0 && (
          <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <p className="text-muted small mb-0">
              Page {pageNumber} of {totalPages} ({total} clients)
            </p>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
          </div>
        )}
      </Card>
      <CustomerBookingsModal
        isOpen={selectedCustomer !== null}
        customerId={selectedCustomer?.id ?? null}
        customerName={selectedCustomer?.name ?? ''}
        onClose={() => setSelectedCustomer(null)}
      />
    </div>
  )
}
