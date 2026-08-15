export const TenantRequestStatus = {
  Pending: 'Pending',
  Approved: 'Approved',
  Rejected: 'Rejected',
} as const

export type TenantRequestStatus = (typeof TenantRequestStatus)[keyof typeof TenantRequestStatus]

// ApexBooking.Core.Domain.Enums.TenantRequestStatus values (pending/approved/rejected) as sent
// over the wire by PendingTenantRequestSummary.Status (enum.ToString(), lowercase).
export function fromWireStatus(wireStatus: string): TenantRequestStatus {
  switch (wireStatus.toLowerCase()) {
    case 'approved':
      return TenantRequestStatus.Approved
    case 'rejected':
      return TenantRequestStatus.Rejected
    default:
      return TenantRequestStatus.Pending
  }
}
