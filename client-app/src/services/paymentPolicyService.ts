import { authClient } from '../api/clients/authClient'
import type { IPaymentPolicy } from '../interfaces/IPaymentPolicy'

// Wire shape from ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentPolicy.PaymentPolicyDto
// matches IPaymentPolicy field-for-field (camelCase JSON naming policy), so no mapper is needed.

export async function getPaymentPolicy(): Promise<IPaymentPolicy> {
  const response = await authClient.get<IPaymentPolicy>('/api/Tenant/policy/payment')
  return response.data
}

export async function updatePaymentPolicy(values: IPaymentPolicy): Promise<void> {
  await authClient.put('/api/Tenant/policy/payment', values)
}
