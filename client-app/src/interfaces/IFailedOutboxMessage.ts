// Mirrors ApexBooking.Core.Application.Features.Platform.Queries.GetFailedOutboxMessages.FailedOutboxMessageSummary
export interface IFailedOutboxMessage {
  id: string
  eventType: string
  lastError: string | null
  retryCount: number
  occurredAtUtc: string
}
