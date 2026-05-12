using ApexBooking.Core.Application.Features.Settings.Commands.UpdateTenantPaymentPolicy;
using ApexBooking.Core.Application.Features.Settings.Commands.UpdateTenantSettings;
using ApexBooking.Core.Application.Features.Settings.Queries.GetPayPaylSettings;
using ApexBooking.Core.Application.Features.Settings.Queries.GetTenantPaymentPolicy;
using ApexBooking.Core.Application.Features.Settings.Queries.GetTenantSettings;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("tenant")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetTenantSettings(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTenantSettingsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("payment-gateway")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPaymentSettings(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPayPalSettingsQuery(), ct);
        return Ok(result);
    }

    [HttpPatch("tenant")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateTenantSettings(
        [FromBody] UpdateTenantSettingsRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateTenantSettingsCommand(
            dto.BookingConfirmationMode,
            dto.MinAdvanceBookingHours,
            dto.MaxAdvanceBookingDays,
            dto.CancellationCutoffHours,
            dto.LateCancellationPolicy,
            dto.GuestBookingEnabled,
            dto.NotifyBookingConfirmed,
            dto.NotifyBookingCancelled,
            dto.NotifyBookingReminder,
            dto.NotifyNewCustomer,
            dto.ReminderHoursBefore), ct);

        return Ok(result);
    }

    [HttpGet("payment-policy")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetPaymentPolicy(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTenantPaymentPolicyQuery(), ct);
        return Ok(result);
    }

    [HttpPatch("payment-policy")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePaymentPolicy(
        [FromBody] UpdateTenantPaymentPolicyRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateTenantPaymentPolicyCommand(
            dto.PaymentRequired,
            dto.DepositOnly,
            dto.DepositType,
            dto.DepositValue,
            dto.RefundPercent), ct);

        return Ok(result);
    }
}
