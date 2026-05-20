using ApexBooking.Core.Application.Features.Availability.Queries;
using ApexBooking.WebApi.Dtos;
using ApexBooking.Core.Application.Features.Bookings.Commands.CancelBookingByToken;
using ApexBooking.Core.Application.Features.Public.Queries.GetMonthlyAvailability;
using ApexBooking.Core.Application.Features.Public.Queries.GetPublicResources;
using ApexBooking.Core.Application.Features.Public.Queries.GetPublicServices;
using ApexBooking.Core.Application.Features.Public.Queries.GetPublicTenant;
using ApexBooking.SharedKernel.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApexBooking.Core.Application.Features.Public.Queries.GetPublicBookingById;
using ApexBooking.Core.Application.Features.Public.Commands.SubmitTenantRequest;
using ApexBooking.Core.Application.Features.Bookings.Queries.ValidateCancellationToken;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetPublicTenant(string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicTenantQuery(slug), ct);
        return Ok(result);
    }

    [HttpGet("{slug}/services")]
    public async Task<IActionResult> GetPublicServices([FromQuery] QueryObjectParams param,string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicServicesQuery(param, slug), ct);
        return Ok(result);
    }

    [HttpGet("{slug}/services/{serviceId:guid}/resources")]
    public async Task<IActionResult> GetPublicResources(
        string slug,
        Guid serviceId,
        [FromQuery] DateOnly? date,
        [FromQuery] TimeOnly? time,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicResourcesQuery(slug, serviceId, date, time), ct);
        return Ok(result);
    }

    [HttpGet("services/{serviceId}/slots")]
    public async Task<IActionResult> GetAvailableSlots(
           Guid serviceId,
           [FromQuery] Guid? resourceId,
           [FromQuery] DateOnly date,
           [FromQuery] string slug,
           CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAvailableSlotsQuery(slug, serviceId, resourceId, date), ct);

        return Ok(result);
    }

    [HttpGet("{slug}/services/{serviceId:guid}/monthly-availability")]
    public async Task<IActionResult> GetMonthlyAvailability(string slug, Guid serviceId, int year, int month, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMonthlyAvailabilityQuery(slug, serviceId, year, month), ct);
        return Ok(result);
    }

    [HttpGet("{slug}/bookings/{bookingId:guid}")]
    public async Task<IActionResult> GetPublicBooking(string slug, Guid bookingId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicBookingByIdQuery(slug, bookingId), ct);
        return Ok(result);
    }

    [HttpGet("cancellation/validate")]
    public async Task<IActionResult> ValidateCancellationToken([FromQuery] string token, CancellationToken ct)
    {
        var result = await _mediator.Send(new ValidateCancellationTokenQuery(token), ct);
        return Ok(result);
    }

    [HttpPost("cancellation/cancel")]
    public async Task<IActionResult> CancelByToken([FromBody] GuestCancelRequestDto dto, CancellationToken ct)
    {
        await _mediator.Send(new CancelBookingByTokenCommand(dto.Token, dto.Reason), ct);
        return NoContent();
    }

    [HttpPost("tenant-requests")]
    public async Task<IActionResult> SubmitTenantRequest(
        [FromBody] SubmitTenantRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitTenantRequestCommand(
            dto.BusinessName,
            dto.OwnerFullName,
            dto.OwnerEmail,
            dto.OwnerPhone,
            dto.Plan,
            dto.Message), ct);

        return Ok(result);
    }
}