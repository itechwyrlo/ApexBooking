import { useState } from 'react'
import { PageHeader } from '../../components/common/PageHeader'
import { Card } from '../../components/common/Card'
import { FailedOutboxMessageTable } from '../../components/admin/FailedOutboxMessageTable'
import { useFailedOutboxMessages } from '../../hooks/useFailedOutboxMessages'
import { useToast } from '../../hooks/useToast'
import { retryOutboxMessage } from '../../services/superAdminService'
import type { IFailedOutboxMessage } from '../../interfaces/IFailedOutboxMessage'

export function FailedNotificationsPage() {
  const { messages, isLoading, refetch } = useFailedOutboxMessages()
  const { showToast } = useToast()
  const [retryingId, setRetryingId] = useState<string | null>(null)

  const handleRetry = async (message: IFailedOutboxMessage) => {
    setRetryingId(message.id)
    try {
      await retryOutboxMessage(message.id)
      showToast('success', 'Retry queued — it will process within a few seconds.')
      refetch()
    } catch {
      showToast('error', 'Failed to queue the retry.')
    } finally {
      setRetryingId(null)
    }
  }

  return (
    <div>
      <PageHeader
        title="Failed Notifications"
        description="Notification and email deliveries that exhausted their automatic retries. Retry them manually once the underlying issue is resolved."
      />
      <Card>
        <FailedOutboxMessageTable
          messages={messages}
          isLoading={isLoading}
          retryingId={retryingId}
          onRetry={handleRetry}
        />
      </Card>
    </div>
  )
}
