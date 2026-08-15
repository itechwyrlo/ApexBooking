// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentGatewayCredential.PaymentGatewayCredentialDto
export interface IPaymentGatewayCredential {
  publicKey: string | null
  maskedSecretKey: string | null
  mode: 'Test' | 'Live' | null
  isConfigured: boolean
  isWebhookConfigured: boolean
}

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentGateway.ConfigurePaymentGatewayCommand
export interface IPaymentGatewayCredentialValues {
  secretKey: string
  publicKey: string
}

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentGateway.SetPaymentGatewayWebhookSecretCommand
export interface IWebhookSecretValues {
  webhookSecret: string
}
