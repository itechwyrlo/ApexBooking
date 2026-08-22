using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ApexBooking.Core.Domain.Services.Paymongo;

namespace ApexBooking.Infrastructure.ExternalServices.PayMongo
{
    /// <summary>
    /// Verifies the "Paymongo-Signature" header PayMongo attaches to webhook requests, in the
    /// format "t=&lt;timestamp&gt;,te=&lt;test_signature&gt;,li=&lt;live_signature&gt;" — the
    /// signed payload is "{t}.{rawBody}", HMAC-SHA256'd with the tenant's whsk_... secret.
    /// </summary>
    public sealed class PayMongoWebhookSignatureVerifier : IPayMongoWebhookSignatureVerifier
    {
        public bool Verify(string rawBody, string? signatureHeader, string webhookSecret, bool useLiveMode)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(webhookSecret))
                return false;

            var parts = ParseSignatureHeader(signatureHeader);

            if (!parts.TryGetValue("t", out var timestamp) || string.IsNullOrEmpty(timestamp))
                return false;

            var signatureKey = useLiveMode ? "li" : "te";
            if (!parts.TryGetValue(signatureKey, out var providedSignatureHex) || string.IsNullOrEmpty(providedSignatureHex))
                return false;

            byte[] providedSignature;
            try
            {
                providedSignature = Convert.FromHexString(providedSignatureHex);
            }
            catch (FormatException)
            {
                return false;
            }

            var signedPayload = $"{timestamp}.{rawBody}";
            var expectedSignature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(webhookSecret), Encoding.UTF8.GetBytes(signedPayload));

            // Fixed-time comparison blocks a timing-attack oracle against the signature bytes —
            // same reasoning as HmacTicketTokenService's ticket signature check.
            return providedSignature.Length == expectedSignature.Length
                && CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature);
        }

        public bool TryGetTimestamp(string? signatureHeader, out DateTimeOffset timestamp)
        {
            timestamp = default;

            if (string.IsNullOrWhiteSpace(signatureHeader))
                return false;

            var parts = ParseSignatureHeader(signatureHeader);
            if (!parts.TryGetValue("t", out var raw) || !long.TryParse(raw, out var unixSeconds))
                return false;

            timestamp = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return true;
        }

        private static Dictionary<string, string> ParseSignatureHeader(string signatureHeader)
        {
            var result = new Dictionary<string, string>();

            foreach (var segment in signatureHeader.Split(','))
            {
                var separatorIndex = segment.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = segment[..separatorIndex].Trim();
                var value = segment[(separatorIndex + 1)..].Trim();
                result[key] = value;
            }

            return result;
        }
    }
}
