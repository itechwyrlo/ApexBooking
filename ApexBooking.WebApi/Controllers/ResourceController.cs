using ApexBooking.Core.Application.Features.Resources.Commands.DeactivateResource;
using ApexBooking.Core.Application.Features.Resources.Commands.UpdateResource;
using ApexBooking.Core.Application.Features.Resources.Queries.GetResourceById;
using ApexBooking.Core.Application.Features.Resources.Queries.GetResourceExceptions;
using ApexBooking.Core.Application.Features.Resources.Queries.GetResources;
using ApexBooking.Core.Application.Features.Availability.Commands.CreateResource;
using ApexBooking.Core.Application.Features.Availability.Commands.SetResourceAvailability;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApexBooking.Core.Application.Features.Availability.Commands.RemoveException;
using ApexBooking.Core.Application.Features.Availability.Commands.AddException;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Application.Features.Resources.Queries.GetResourceAvailability;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourceController : ControllerBase
{
    private readonly ILogger<ResourceController> _logger;
    private readonly IMediator _mediator;

    public ResourceController(IMediator mediator, ILogger<ResourceController> logger)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet()]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] QueryObjectParams param)
    {
        var result = await _mediator.Send(new GetResourcesQuery(param));
        return Ok(result);
    }

    [HttpGet("{resourceId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid resourceId)
    {
        var result = await _mediator.Send(new GetResourceByIdQuery(resourceId));
        return Ok(result);
    }

    [HttpPost()]
    [Authorize]
    public async Task<IActionResult> Create(CreateResourceRequestDto dto)
    {
        if (!Enum.IsDefined(typeof(ResourceType), dto.ResourceType))
            throw new BusinessRuleBrokenException("Invalid resource type.");

        var response = await _mediator.Send(new CreateResourceCommand(
            dto.Name,
            (ResourceType)dto.ResourceType,
            dto.Capacity,
            dto.Description
        ));

        return CreatedAtAction(nameof(GetById), new { resourceId = response.Data.ResourceId.Value }, response);
    }

    [HttpPatch("{resourceId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid resourceId, [FromBody] UpdateResourceRequestDto dto)
    {
        var response = await _mediator.Send(new UpdateResourceCommand(
            resourceId,
            dto.Name,
            dto.Description,
            dto.Capacity
        ));

        return Ok(response);
    }

    [HttpPatch("{resourceId:guid}/status")]
    [Authorize]
    public async Task<IActionResult> Deactivate(Guid resourceId)
    {
        await _mediator.Send(new DeactivateResourceCommand(resourceId));
        return NoContent();
    }

    [HttpGet("{resourceId:guid}/exceptions")]
    [Authorize]
    public async Task<IActionResult> GetExceptions([FromQuery] QueryObjectParams param, Guid resourceId)
    {
        var result = await _mediator.Send(new GetResourceExceptionsQuery(param, resourceId));
        return Ok(result);
    }

    [HttpGet("{resourceId:guid}/availability")]
    [Authorize]
    public async Task<IActionResult> GetAvailability(Guid resourceId)
    {
        var result = await _mediator.Send(new GetResourceAvailabilityQuery(resourceId));
        return Ok(result);
    }

    [HttpPost("{resourceId:guid}/exceptions")]
    [Authorize]
    public async Task<IActionResult> AddException(Guid resourceId, [FromBody] AddExceptionRequestDto dto)
    {
        if (!Enum.TryParse<ExceptionType>(dto.ExceptionType, out var exceptionType))
            throw new BusinessRuleBrokenException("Invalid exception type.");

        var response = await _mediator.Send(new AddExceptionCommand(
            resourceId,
            dto.ExceptionDate,
            exceptionType,
            dto.StartTime,
            dto.EndTime,
            dto.Note
        ));

        return Ok(response);
    }

    [HttpDelete("{resourceId:guid}/exceptions/{exceptionId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveException(Guid resourceId, Guid exceptionId)
    {
        var response = await _mediator.Send(new RemoveExceptionCommand(resourceId, exceptionId));
        return Ok(response);
    }

    [HttpPut("{resourceId}/availability")]
    [Authorize]
    public async Task<IActionResult> SetAvailability(Guid resourceId, [FromBody] SetResourceAvailabilityRequestDto request)
    {

        await _mediator.Send(new SetResourceAvailabilityCommand(
            resourceId,
            request.Schedules
        ));
        return NoContent();
    }
}