namespace ApexBooking.Core.Domain.Interfaces;

public interface IPayPalWebhookValidator
{
    Task<bool> ValidateAsync(
        string clientId,
        string secretKey,
        string webhookId,
        string requestBody,
        string transmissionId,
        string transmissionTime,
        string certUrl,
        string authAlgo,
        string transmissionSig,
        CancellationToken cancellationToken = default);
}