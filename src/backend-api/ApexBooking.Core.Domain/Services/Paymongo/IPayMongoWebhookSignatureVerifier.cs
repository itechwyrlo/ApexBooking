using System;

namespace ApexBooking.Core.Domain.Services.Paymongo
{
    public interface IPayMongoWebhookSignatureVerifier
    {
        /// <summary>
        /// Verifies a PayMongo webhook request against the tenant's own webhook signing secret.
        /// </summary>
        /// <param name="rawBody">The exact, unmodified request body bytes PayMongo signed.</param>
        /// <param name="signatureHeader">The raw "Paymongo-Signature" header value.</param>
        /// <param name="webhookSecret">The tenant's whsk_... signing secret.</param>
        /// <param name="useLiveMode">Whether to check the live-mode signature segment (li=) instead of test-mode (te=).</param>
        bool Verify(string rawBody, string? signatureHeader, string webhookSecret, bool useLiveMode);

        /// <summary>
        /// Extracts the "t=" timestamp segment from the signature header, independent of whether
        /// the signature itself verifies — used to reject stale/replayed deliveries.
        /// </summary>
        bool TryGetTimestamp(string? signatureHeader, out DateTimeOffset timestamp);
    }
}
