using ApexBooking.Core.Application.Features.Availability.Commands.AddException;
using ApexBooking.Core.Application.Features.Availability.Commands.RemoveException;
using ApexBooking.Core.Application.Features.Availability.Commands.SetResourceAvailability;
using ApexBooking.Core.Application.Features.Staffs.Commands.CreateStaff;
using ApexBooking.Core.Application.Features.Staffs.Commands.DeactivateStaff;
using ApexBooking.Core.Application.Features.Staffs.Commands.UpdateMyPhoto;
using ApexBooking.Core.Application.Features.Staffs.Commands.UpdateStaff;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetMyProfile;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffAvailability;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffById;
using ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffExceptions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly ILogger<StaffController> _logger;
    private readonly IMediator _mediator;

    public StaffController(IMediator mediator, ILogger<StaffController> logger)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("me")]
    [Authorize(Roles = "staff")]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _mediator.Send(new GetMyProfileQuery());
        return Ok(result);
    }

    [HttpPut("me/photo")]
    [Authorize(Roles = "staff")]
    public async Task<IActionResult> UpdateMyPhoto([FromBody] UpdateMyPhotoRequestDto dto)
    {
        var result = await _mediator.Send(new UpdateMyPhotoCommand(dto.PhotoUrl));
        return Ok(result);
    }

    [HttpGet()]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] QueryObjectParams param)
    {
        var result = await _mediator.Send(new GetStaffQuery(param));
        return Ok(result);
    }

    [HttpGet("{resourceId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid staffId)
    {
        var result = await _mediator.Send(new GetStaffByIdQuery(staffId));
        return Ok(result);
    }

    [HttpPost()]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequestDto dto)
    {
        var staff = await _mediator.Send(new CreateStaffCommand(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.ContactNumber,
            dto.Capacity,
            dto.Description
        ));

        return CreatedAtAction(nameof(GetById), new { resourceId = staff.Id }, staff);
    }

    [HttpPatch("{staffId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid staffId, [FromBody] UpdateResourceRequestDto dto)
    {
        var response = await _mediator.Send(new UpdateStaffCommand(
            staffId,
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.ContactNumber,
            dto.Description,
            dto.Capacity
        ));

        return Ok(response);
    }

    [HttpPatch("{staffId:guid}/status")]
    [Authorize]
    public async Task<IActionResult> Deactivate(Guid staffId)
    {
        await _mediator.Send(new DeactivateStaffCommand(staffId));
        return NoContent();
    }

    [HttpGet("{staffId:guid}/exceptions")]
    [Authorize]
    public async Task<IActionResult> GetExceptions([FromQuery] QueryObjectParams param, Guid staffId)
    {
        var result = await _mediator.Send(new GetStaffExceptionsQuery(param, staffId));
        return Ok(result);
    }

    [HttpGet("{staffId:guid}/availability")]
    [Authorize]
    public async Task<IActionResult> GetAvailability(Guid staffId)
    {
        var result = await _mediator.Send(new GetStaffAvailabilityQuery(staffId));
        return Ok(result);
    }

    [HttpPost("{staffId:guid}/exceptions")]
    [Authorize]
    public async Task<IActionResult> AddException(Guid staffId, [FromBody] AddExceptionRequestDto dto)
    {
        if (!Enum.TryParse<ExceptionType>(dto.ExceptionType, out var exceptionType))
            throw new BusinessRuleBrokenException("Invalid exception type.");

        await _mediator.Send(new AddExceptionCommand(
            staffId,
            dto.ExceptionDate,
            exceptionType,
            dto.StartTime,
            dto.EndTime,
            dto.Note
        ));

        return NoContent();
    }

    [HttpDelete("{staffId:guid}/exceptions/{exceptionId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveException(Guid staffId, Guid exceptionId)
    {
        await _mediator.Send(new RemoveExceptionCommand(staffId, exceptionId));
        return NoContent();
    }

    [HttpPut("{staffId}/availability")]
    [Authorize]
    public async Task<IActionResult> SetAvailability(Guid staffId, [FromBody] SetResourceAvailabilityRequestDto request)
    {

        await _mediator.Send(new SetResourceAvailabilityCommand(
            staffId,
            request.Schedules
        ));
        return NoContent();
    }
}