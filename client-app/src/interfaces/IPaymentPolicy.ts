import type { PaymentRequirementType } from '../types/PaymentRequirementType'
import type { DepositType } from '../types/DepositType'

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentPolicy.PaymentPolicyDto
export interface IPaymentPolicy {
  requirementType: PaymentRequirementType
  depositType: DepositType
  depositValue: number
  onTimeRefundPercent: number
  lateCancellationRefundPercent: number
  refundReviewDeadlineDays: number
  refundEnabled: boolean
}
