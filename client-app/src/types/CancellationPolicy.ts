export const CancellationPolicy = {
  NoRefund: 'NoRefund',
  PartialRefund: 'PartialRefund',
  FullRefund: 'FullRefund',
} as const

export type CancellationPolicy = (typeof CancellationPolicy)[keyof typeof CancellationPolicy]
