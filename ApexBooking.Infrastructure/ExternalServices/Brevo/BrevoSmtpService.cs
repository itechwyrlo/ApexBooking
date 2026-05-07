using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Services.Notification;
using ApexBooking.Infrastructure.Configuration;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
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
            var client = _httpClientFactory.CreateClient();

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

            var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", stringContent);

            var error = await response.Content.ReadAsStringAsync();
        }
    }
}