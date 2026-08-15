import { authClient } from '../api/clients/authClient'
import type { IPaymentGatewayCredential, IPaymentGatewayCredentialValues, IWebhookSecretValues } from '../interfaces/IPaymentGatewayCredential'

// Wire shape from ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentGatewayCredential.PaymentGatewayCredentialDto
// matches IPaymentGatewayCredential field-for-field (camelCase JSON naming policy), so no mapper is needed.

export async function getPaymentGatewayCredential(): Promise<IPaymentGatewayCredential> {
  const response = await authClient.get<IPaymentGatewayCredential>('/api/Tenant/payment-gateway')
  return response.data
}

export async function configurePaymentGateway(values: IPaymentGatewayCredentialValues): Promise<void> {
  await authClient.put('/api/Tenant/payment-gateway', values)
}

export async function setPaymentGatewayWebhookSecret(values: IWebhookSecretValues): Promise<void> {
  await authClient.put('/api/Tenant/payment-gateway/webhook-secret', values)
}
