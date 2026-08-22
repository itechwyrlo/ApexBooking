using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.Bookings.Commands.ProcessPaymentWebhook;
using ApexBooking.Core.Domain.Services.Paymongo;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/public/webhooks")]
    [AllowAnonymous]
    public class PayMongoWebhooksController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PayMongoWebhooksController> _logger;

        public PayMongoWebhooksController(IMediator mediator, ILogger<PayMongoWebhooksController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("paymongo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandlePayMongoCallback()
        {
            // 1. Read the raw text stream body message directly from the network payload frame
            using var reader = new StreamReader(Request.Body);
            var jsonText = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(jsonText))
                return BadRequest("Empty payload context.");

            try
            {
                // 2. Deserialize the payload utilizing standard loose naming policies
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var payload = JsonSerializer.Deserialize<PayMongoWebhookPayload>(jsonText, options);

                if (payload?.Data?.Attributes?.Data?.Attributes == null)
                    return BadRequest("Invalid or broken structural payload metadata schema received.");

                var resourceAttributes = payload.Data.Attributes.Data.Attributes;

                // 3. Verify that the payment transaction status registers as fully "paid" before moving forward
                if (resourceAttributes.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                {
                    // Raw body + signature header travel down to the handler, which verifies them
                    // against the tenant's own webhook secret before trusting anything above.
                    var signatureHeader = Request.Headers["Paymongo-Signature"].FirstOrDefault();
                    // The Payment resource's real id lives nested under the paid Link's own
                    // `payments` array, not at the link's top-level `id` (that's the Link's own
                    // id, e.g. "link_..." — confirmed via sandbox webhook capture 2026-08-11).
                    var payment = resourceAttributes.Payments.FirstOrDefault();
                    var payMongoPaymentId = payment?.Data.Id;
                    var paidAmountInCentavos = payment?.Data.Attributes.AmountInCentavos;
                    await _mediator.Send(new ProcessPaymentWebhookCommand(
                        resourceAttributes.Remarks, payMongoPaymentId, jsonText, signatureHeader, payload.Data.Id, paidAmountInCentavos));
                }

                // 4. Return an immediate standard HTTP 200 OK success response back to PayMongo's servers.
                // This signals that your backend safely captured the event, preventing PayMongo from continually resending the alert.
                return Ok();
            }
            catch (Exception ex)
            {
                // DPA: never log the raw webhook payload (or any header) — it can carry
                // customer-identifying and payment-adjacent data. A SHA-256 hash plus a fresh
                // correlation id is enough to match this failure against PayMongo's own delivery
                // logs or a support ticket without persisting the content itself.
                var correlationId = Guid.NewGuid();
                var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(jsonText)));

                _logger.LogError(ex,
                    "Failed to process PayMongo webhook callback. CorrelationId: {CorrelationId}, BodySha256: {BodyHash}",
                    correlationId, bodyHash);

                // Fail safely: Deliver an HTTP 400 bad request indicator to trigger an automatic gateway fallback loop
                return BadRequest($"Failed to process payment callback event cleanly. Reference: {correlationId}");
            }
        }
    }
}
