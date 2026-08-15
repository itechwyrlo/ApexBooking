export const DepositType = {
  Percentage: 'Percentage',
  FixedAmount: 'FixedAmount',
} as const

export type DepositType = (typeof DepositType)[keyof typeof DepositType]
