using ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking;
using ApexBooking.Core.Application.Features.Bookings.Commands.CreateBooking;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetBookingById;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetBookings;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetBookingsQuery());
        return Ok(result);
    }

    [HttpGet("{bookingId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid bookingId)
    {
        var result = await _mediator.Send(new GetBookingByIdQuery(bookingId));
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequestDto dto)
    {
        var result = await _mediator.Send(new CreateBookingCommand(
            dto.ServiceId,
            dto.ResourceId,
            dto.ScheduledDate,
            dto.ScheduledStartTime,
            dto.CustomerNotes
        ));

        return CreatedAtAction(nameof(GetById), new { bookingId = result.Data.BookingId }, result);
    }

    [HttpPost("{bookingId:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid bookingId, [FromBody] CancelBookingRequestDto dto)
    {
        var result = await _mediator.Send(new CancelBookingCommand(
            bookingId,
            dto.Reason
        ));

        return Ok(result);
    }
}