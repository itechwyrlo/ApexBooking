using ApexBooking.Core.Application.Features.Availability.Queries;
using ApexBooking.Core.Application.Features.Public.Queries.GetPublicResources;
using ApexBooking.Core.Application.Features.Public.Queries.GetPublicServices;
using ApexBooking.Core.Application.Features.Public.Queries.GetPublicTenant;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetPublicServices(string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicServicesQuery(slug), ct);
        return Ok(result);
    }

    [HttpGet("{slug}/services/{serviceId:guid}/resources")]
    public async Task<IActionResult> GetPublicResources(string slug, Guid serviceId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicResourcesQuery(slug, serviceId), ct);
        return Ok(result);
    }

    [HttpGet("services/{serviceId}/slots")]
    public async Task<IActionResult> GetAvailableSlots(
           Guid serviceId,
           [FromQuery] Guid resourceId,
           [FromQuery] DateOnly date,
           [FromQuery] string slug,
           CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAvailableSlotsQuery(slug, serviceId, resourceId, date), ct);

        return Ok(result);
    }
}