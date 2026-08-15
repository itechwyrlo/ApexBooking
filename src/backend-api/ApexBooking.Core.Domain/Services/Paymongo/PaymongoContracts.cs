using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.Paymongo
{
    public class PayMongoLinkRequest
    {
        [JsonPropertyName("data")]
        public LinkData Data { get; set; } = new();
    }

    public class LinkData
    {
        [JsonPropertyName("attributes")]
        public LinkAttributes Attributes { get; set; } = new();
    }

    public class LinkAttributes
    {
        // 🌟 PayMongo tracks currency values completely as integers in Centavos (PHP * 100)
        [JsonPropertyName("amount")]
        public long AmountInCentavos { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        // 🌟 Pass-Through Tracking Token: We pass "BOOKING_{Guid}" here.
        // The webhook will reflect this back, so our system can identify the appointment instantly.
        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;
    }

    public class PayMongoLinkResponse
    {
        [JsonPropertyName("data")]
        public LinkResponseData Data { get; set; } = new();
    }

    public class LinkResponseData
    {
        [JsonPropertyName("attributes")]
        public LinkResponseAttributes Attributes { get; set; } = new();
    }

    public class LinkResponseAttributes
    {
        [JsonPropertyName("checkout_url")]
        public string CheckoutUrl { get; set; } = string.Empty;
    }


    public class PayMongoWebhookPayload
    {
        [JsonPropertyName("data")]
        public WebhookData Data { get; set; } = new();
    }

    public class WebhookData
    {
        [JsonPropertyName("attributes")]
        public WebhookAttributes Attributes { get; set; } = new();
    }

    public class WebhookAttributes
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // e.g., "link.payment.paid"

        [JsonPropertyName("data")]
        public WebhookResource Data { get; set; } = new();
    }

    public class WebhookResource
    {
        // Confirmed by a real sandbox webhook capture (2026-08-11): this is the Legacy Link's own
        // ID (e.g. "link_..."), NOT the Payment resource's ID — despite this field's former doc
        // comment claiming otherwise. The actual Payment ID lives nested under
        // Attributes.Payments[0].Data.Id (e.g. "pay_..."). Kept here as the link's own identifier;
        // do not use this for refund calls.
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public ResourceAttributes Attributes { get; set; } = new();
    }

    public class ResourceAttributes
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // e.g., "paid"

        // 🌟 This maps directly back to the "BOOKING_{Guid}" pass-through tracking token
        // we safely attached during our InitiateBookingHandler generation phase!
        [JsonPropertyName("remarks")]
        public string Remarks { get; set; } = string.Empty;

        // A paid Link carries its actual settled Payment(s) here — this is where the real
        // "pay_..." id needed for refunds actually lives (confirmed via sandbox webhook capture,
        // 2026-08-11). Empty for a not-yet-paid link.
        [JsonPropertyName("payments")]
        public List<LinkPayment> Payments { get; set; } = new();
    }

    public class LinkPayment
    {
        [JsonPropertyName("data")]
        public LinkPaymentData Data { get; set; } = new();
    }

    public class LinkPaymentData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty; // e.g. "pay_..." — the real PayMongo Payment resource ID

        [JsonPropertyName("attributes")]
        public LinkPaymentAttributes Attributes { get; set; } = new();
    }

    public class LinkPaymentAttributes
    {
        // Confirmed via real sandbox webhook capture (2026-08-12): this is where the payment
        // method actually lives — e.g. "type": "qrph", "type": "gcash". Not to be confused with
        // the outer WebhookAttributes.Type ("link.payment.paid", the event type).
        [JsonPropertyName("source")]
        public PaymentSource Source { get; set; } = new();
    }

    public class PaymentSource
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // e.g. "qrph", "gcash", "card"
    }

    public class PayMongoRefundRequest
    {
        [JsonPropertyName("data")]
        public RefundData Data { get; set; } = new();
    }

    public class RefundData
    {
        [JsonPropertyName("attributes")]
        public RefundAttributes Attributes { get; set; } = new();
    }

    public class RefundAttributes
    {
        [JsonPropertyName("amount")]
        public long AmountInCentavos { get; set; }

        [JsonPropertyName("payment_id")]
        public string PaymentId { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty; // one of: duplicate, fraudulent, requested_by_customer, others

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class PayMongoRefundResponse
    {
        [JsonPropertyName("data")]
        public RefundResponseData Data { get; set; } = new();
    }

    public class RefundResponseData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public RefundResponseAttributes Attributes { get; set; } = new();
    }

    public class RefundResponseAttributes
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // pending | processing | succeeded | failed
    }
}