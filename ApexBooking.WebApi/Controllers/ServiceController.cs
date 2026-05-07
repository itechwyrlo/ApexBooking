using ApexBooking.Core.Application.Features.Availability.Queries;
using ApexBooking.Core.Application.Features.service;
using ApexBooking.Core.Application.Features.Services.Commands.CreateService;
using ApexBooking.Core.Application.Features.Services.Commands.DeactivateService;
using ApexBooking.Core.Application.Features.Services.Commands.UpdateService;
using ApexBooking.Core.Application.Features.Services.Queries.GetServiceById;
using ApexBooking.Core.Application.Features.Services.Queries.GetServices;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServiceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetServicesQuery());
        return Ok(result);
    }

    [HttpGet("{serviceId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid serviceId)
    {
        var result = await _mediator.Send(new GetServiceByIdQuery(serviceId));
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestDto dto)
    {
        var response = await _mediator.Send(new CreateServiceCommand(
            dto.Name,
            dto.Description,
            dto.DurationMinutes,
            dto.Price,
            dto.CurrencyCode,
            dto.ResourceIds,
            dto.BufferBeforeMinutes,
            dto.BufferAfterMinutes,
            dto.MinAdvanceBookingHours,
            dto.MaxAdvanceBookingDays
        ));

        return CreatedAtAction(nameof(GetById), new { serviceId = response.Data.Id }, response);
    }

    [HttpPatch("{serviceId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid serviceId, [FromBody] UpdateServiceRequestDto dto)
    {
        var response = await _mediator.Send(new UpdateServiceCommand(
            serviceId,
            dto.Name,
            dto.Description,
            dto.DurationMinutes,
            dto.Price,
            dto.CurrencyCode,
            dto.ResourceIds,
            dto.BufferBeforeMinutes,
            dto.BufferAfterMinutes,
            dto.MinAdvanceBookingHours,
            dto.MaxAdvanceBookingDays
        ));

        return Ok(response);
    }

    [HttpPatch("{serviceId:guid}/status")]
    [Authorize]
    public async Task<IActionResult> Deactivate(Guid serviceId)
    {
        var response = await _mediator.Send(new DeactivateServiceCommand(serviceId));
        return Ok(response);
    }

   
}