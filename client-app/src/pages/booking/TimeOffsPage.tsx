import { useState } from 'react'
import axios from 'axios'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { Button } from '../../components/common/Button'
import { Pagination } from '../../components/common/Pagination'
import { TimeOffTable } from '../../components/team/TimeOffTable'
import { RequestTimeOffModal } from '../../components/team/RequestTimeOffModal'
import { useTimeOffRequests } from '../../hooks/useTimeOffRequests'
import { useAuth } from '../../hooks/useAuth'
import { useToast } from '../../hooks/useToast'
import { approveTimeOff, rejectTimeOff } from '../../services/timeOffService'
import { Role } from '../../types/Role'
import { TimeOffStatus } from '../../types/TimeOffStatus'
import type { ITimeOffFilters, ITimeOffRequest } from '../../interfaces/ITimeOffRequest'

const PAGE_SIZE = 10

export function TimeOffsPage() {
  const { user } = useAuth()
  const isManagement = user !== null && user.roles.includes(Role.Owner)

  const [filters, setFilters] = useState<ITimeOffFilters>({})
  const [pageNumber, setPageNumber] = useState(1)
  const [isRequestModalOpen, setIsRequestModalOpen] = useState(false)
  const [pendingRequestId, setPendingRequestId] = useState<string | null>(null)

  const { requests, total, isLoading, error, refetch } = useTimeOffRequests(filters, { pageNumber, pageSize: PAGE_SIZE })
  const { showToast } = useToast()

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  const handleFilterChange = (next: ITimeOffFilters) => {
    setFilters(next)
    setPageNumber(1)
  }

  const runReviewAction = async (request: ITimeOffRequest, action: () => Promise<void>, successMessage: string) => {
    setPendingRequestId(request.requestId)
    try {
      await action()
      showToast('success', successMessage)
      refetch()
    } catch (actionError) {
      const detail = axios.isAxiosError(actionError)
        ? (actionError.response?.data as { detail?: string } | undefined)?.detail
        : undefined
      showToast('error', detail ?? 'That action could not be completed. Please try again.')
    } finally {
      setPendingRequestId(null)
    }
  }

  const handleApprove = (request: ITimeOffRequest) =>
    runReviewAction(request, () => approveTimeOff(request.requestId), `Approved ${request.memberName}'s time-off request.`)

  const handleReject = (request: ITimeOffRequest) =>
    runReviewAction(request, () => rejectTimeOff(request.requestId), `Rejected ${request.memberName}'s time-off request.`)

  return (
    <div>
      <PageHeader
        title={isManagement ? 'Team Time Off' : 'My Time Off'}
        description={
          isManagement
            ? 'Review and decide on time-off requests from your whole team.'
            : 'Track your vacation, sick days, and other unavailable time.'
        }
        primaryAction={<Button onClick={() => setIsRequestModalOpen(true)}>Request Time Off</Button>}
      />

      <Card className="mb-3">
        <div className="row g-3 align-items-end">
          <div className="col-6 col-md-3">
            <label className="form-label small text-muted" htmlFor="status-filter">
              Status
            </label>
            <select
              id="status-filter"
              className="form-select"
              value={filters.status ?? ''}
              onChange={(e) => handleFilterChange({ ...filters, status: (e.target.value || undefined) as ITimeOffFilters['status'] })}
            >
              <option value="">All statuses</option>
              {Object.values(TimeOffStatus).map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>
          <div className="col-6 col-md-3">
            <Button variant="outline-secondary" size="sm" disabled={!filters.status} onClick={() => handleFilterChange({})}>
              Clear Filters
            </Button>
          </div>
        </div>
      </Card>

      <Card>
        {error && <p className="text-danger small mb-3">{error}</p>}

        <TimeOffTable
          requests={requests}
          isLoading={isLoading}
          showMemberColumn={isManagement}
          canReview={isManagement}
          pendingRequestId={pendingRequestId}
          onApprove={handleApprove}
          onReject={handleReject}
        />

        {!isLoading && total > 0 && (
          <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3">
            <p className="text-muted small mb-0">
              Page {pageNumber} of {totalPages} ({total} requests)
            </p>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={setPageNumber} />
          </div>
        )}
      </Card>

      <RequestTimeOffModal isOpen={isRequestModalOpen} onClose={() => setIsRequestModalOpen(false)} onRequested={refetch} />
    </div>
  )
}
