export const RefundRequestStatus = {
  PendingReview: 'PendingReview',
  Refunded: 'Refunded',
  Rejected: 'Rejected',
} as const

export type RefundRequestStatus = (typeof RefundRequestStatus)[keyof typeof RefundRequestStatus]
