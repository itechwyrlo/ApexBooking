using System.Text;
using System.Text.Json;
using ApexBooking.Core.Domain.Services.EmailNotification;
using ApexBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ApexBooking.Infrastructure.ExternalServices.Brevo
{
    // Simple DTOs to replace deleted Domain DTOs
    public class BrevoEmailRequest
    {
        public Sender Sender { get; set; }
        public List<Recipient> To { get; set; }
        public string Subject { get; set; }
        public string HtmlContent { get; set; }
    }

    public class Sender
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class Recipient
    {
        public string Email { get; set; }
    }

    public class BrevoSmtpService : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly EmailSettings _emailSettings;
        
        public BrevoSmtpService(IHttpClientFactory httpClientFactory, IConfiguration config, IOptions<EmailSettings> emailSettings)
        {
           _httpClientFactory = httpClientFactory;
           _config = config;
           _emailSettings = emailSettings.Value;
        }
        public async Task SendEmailAsync(string to, string subject, string content)
        {
            // Named client ("Brevo", registered in InfrastructureDependencies with an explicit
            // timeout) rather than the untyped default — a hung Brevo connection would otherwise
            // tie up a Hangfire worker for the default HttpClient timeout (100s) before ever
            // reaching the retry path below.
            var client = _httpClientFactory.CreateClient("Brevo");

            // Set Required Headers
           client.DefaultRequestHeaders.Add("api-key", _config["BrevoSmtp:Key"]);

            var emailRequest = new BrevoEmailRequest
            {
                Sender = new Sender
                {
                    Name = _emailSettings.SenderName,
                    Email = _emailSettings.SenderEmail
                },
                To = new List<Recipient>
                {
                    new Recipient { Email = to }
                },
                Subject = subject,
                HtmlContent = content
            };

            var jsonPayload = JsonSerializer.Serialize(emailRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var stringContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", stringContent);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Network failure or client-side timeout — always worth retrying, Brevo never even
                // saw the request.
                throw new EmailDeliveryException($"Brevo API request failed: {ex.Message}", isTransient: true, ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                var statusCode = (int)response.StatusCode;

                // 429 (rate limited) and 5xx (Brevo-side outage) are worth retrying — the same
                // request will likely succeed once the condition clears. Anything else (400 bad
                // payload, 401 bad API key, malformed recipient, etc.) will fail identically on
                // every retry, so it's classified as permanent — see EmailDeliveryException and
                // OutboxRelayService, which fails those fast instead of spending the outbox
                // message's full retry budget on something that can never succeed.
                var isTransient = statusCode == 429 || statusCode >= 500;

                throw new EmailDeliveryException($"Brevo API error ({statusCode}): {error}", isTransient);
            }
        }
    }
}