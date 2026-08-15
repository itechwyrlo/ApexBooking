export const PaymentRequirementType = {
  None: 'None',
  DepositRequired: 'DepositRequired',
  FullPaymentRequired: 'FullPaymentRequired',
} as const

export type PaymentRequirementType = (typeof PaymentRequirementType)[keyof typeof PaymentRequirementType]
