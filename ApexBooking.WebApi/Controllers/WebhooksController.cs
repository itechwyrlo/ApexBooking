using ApexBooking.Core.Application.Features.Payment.Commands.HandlePayPalWebhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // TR-11.3: PayPal webhook handler. AllowAnonymous because PayPal calls this with no JWT.
    // Signature validation is enforced inside the handler before any state change.
    [HttpPost("payment/paypal")]
    public async Task<IActionResult> HandlePayPal(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var requestBody = await reader.ReadToEndAsync(ct);

        var transmissionId = Request.Headers["paypal-transmission-id"].ToString();
        var transmissionTime = Request.Headers["paypal-transmission-time"].ToString();
        var certUrl = Request.Headers["paypal-cert-url"].ToString();
        var authAlgo = Request.Headers["paypal-auth-algo"].ToString();
        var transmissionSig = Request.Headers["paypal-transmission-sig"].ToString();

        if (string.IsNullOrWhiteSpace(transmissionId) ||
            string.IsNullOrWhiteSpace(transmissionTime) ||
            string.IsNullOrWhiteSpace(certUrl) ||
            string.IsNullOrWhiteSpace(authAlgo) ||
            string.IsNullOrWhiteSpace(transmissionSig))
        {
            return BadRequest();
        }

        await _mediator.Send(new HandlePayPalWebhookCommand(
            requestBody,
            transmissionId,
            transmissionTime,
            certUrl,
            authAlgo,
            transmissionSig), ct);

        return Ok();
    }
}