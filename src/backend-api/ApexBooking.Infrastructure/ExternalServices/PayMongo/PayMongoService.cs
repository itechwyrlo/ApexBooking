using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Services.Paymongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Infrastructure.ExternalServices.PayMongo;

public class PayMongoService : IPayMongoService
{
    private readonly HttpClient _httpClient;

    public PayMongoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.paymongo.com");
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<PayMongoSourceResult> CreatePaymentSourceAsync(
        string tenantSecretKey,
        Guid bookingId,
        Guid branchId,
        decimal amountPhp,
        string description,
        CancellationToken cancellationToken)
    {
        // 1. Scaled Currency Mathematics: Convert PHP to Centavos cleanly to prevent floating-point calculation bugs
        long amountInCentavos = (long)Math.Round(amountPhp * 100, 0);

        // 2. Build the PayMongo Links request payload tracking parameters
        var payMongoRequest = new PayMongoLinkRequest();
        payMongoRequest.Data.Attributes.AmountInCentavos = amountInCentavos;
        payMongoRequest.Data.Attributes.Description = description;
        payMongoRequest.Data.Attributes.Remarks = $"BOOKING_{bookingId}_{branchId}"; //Pass-through verification token prefix

        // 3. Inject the Tenant's Dynamic API Token into the Request Header securely
        var authBytes = Encoding.ASCII.GetBytes($"{tenantSecretKey.Trim()}:");
        var base64Auth = Convert.ToBase64String(authBytes);
        
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/links")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payMongoRequest), 
                Encoding.UTF8, 
                "application/json"
            )
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

        // 4. Dispatch the HTTP payload out to the live PayMongo server network
        var httpResponse = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"PayMongo integration failure. Status: {httpResponse.StatusCode}. Error context: {errorContent}");
        }

        // 5. Unpack and deliver the frontend checkout redirect values
        var responseStream = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var payMongoResult = JsonSerializer.Deserialize<PayMongoLinkResponse>(responseStream);

        if (payMongoResult?.Data?.Attributes == null || string.IsNullOrWhiteSpace(payMongoResult.Data.Attributes.CheckoutUrl))
            throw new Exception("Invalid structural metadata returned from PayMongo API gateway server.");

        string checkoutUrl = payMongoResult.Data.Attributes.CheckoutUrl;

        // PayMongo Links natively support direct scan graphics by adding the '/qr' suffix to the checkout landing path!
        return new PayMongoSourceResult(
            QrCodeUrl: $"{checkoutUrl}/qr",
            CheckoutUrl: checkoutUrl
        );
    }
}
