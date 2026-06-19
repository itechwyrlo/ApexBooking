using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Infrastructure.ExternalServices;

public class PayPalWebhookValidator : IPayPalWebhookValidator
{
    public async Task<bool> ValidateAsync(
        string clientId,
        string secretKey,
        string webhookId,
        string requestBody,
        string transmissionId,
        string transmissionTime,
        string certUrl,
        string authAlgo,
        string transmissionSig,
        CancellationToken cancellationToken = default)
    {
        var isLive = !certUrl.Contains("sandbox");
        var baseUrl = isLive
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        var accessToken = await GetAccessTokenAsync(clientId, secretKey, baseUrl, cancellationToken);
        if (accessToken is null) return false;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var verifyBody = new
        {
            transmission_id = transmissionId,
            transmission_time = transmissionTime,
            cert_url = certUrl,
            auth_algo = authAlgo,
            transmission_sig = transmissionSig,
            webhook_id = webhookId,
            webhook_event = JsonDocument.Parse(requestBody).RootElement
        };

        try
        {
            var response = await client.PostAsync(
                $"{baseUrl}/v1/notifications/verify-webhook-signature",
                new StringContent(
                    JsonSerializer.Serialize(verifyBody),
                    Encoding.UTF8,
                    "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            var status = doc.RootElement.GetProperty("verification_status").GetString();
            return status == "SUCCESS";
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> GetAccessTokenAsync(
        string clientId,
        string secretKey,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{secretKey}"));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        try
        {
            var response = await client.PostAsync(
                $"{baseUrl}/v1/oauth2/token", content, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch
        {
            return null;
        }
    }
}