using ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking;
using ApexBooking.Core.Application.Features.Bookings.Commands.ConfirmBooking;
using ApexBooking.Core.Application.Features.Bookings.Commands.CreateBooking;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetBookingById;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetBookings;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetCalendarBookings;
using ApexBooking.Core.Application.Features.Payment.Commands.InitiatePayment;
using ApexBooking.SharedKernel.Models;
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
    public async Task<IActionResult> GetAll([FromQuery] QueryObjectParams param)
    {
        var result = await _mediator.Send(new GetBookingsQuery(param));
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
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequestDto dto)
    {
        var result = await _mediator.Send(new CreateBookingCommand(
            dto.TenantSlug,
            dto.ServiceId,
            dto.ResourceId,
            dto.ScheduledDate,
            dto.ScheduledStartTime,
            dto.GuestFirstName,
            dto.GuestLastName,
            dto.GuestEmail,
            dto.GuestPhone,
            dto.CustomerNotes
        ));

        return CreatedAtAction(nameof(GetById), new { bookingId = result.BookingId }, result);
    }

    [HttpPost("{bookingId:guid}/confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm(Guid bookingId)
    {
        await _mediator.Send(new ConfirmBookingCommand(bookingId));
        return NoContent();
    }

    [HttpPost("{bookingId:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid bookingId, [FromBody] CancelBookingRequestDto dto)
    {
        await _mediator.Send(new CancelBookingCommand(bookingId, dto.Reason));
        return NoContent();
    }

    [HttpGet("calendar")]
    [Authorize]
    public async Task<IActionResult> GetCalendar([FromQuery] int year, [FromQuery] int month)
    {
        var result = await _mediator.Send(new GetCalendarBookingsQuery(year, month));
        return Ok(result);
    }

    [HttpPost("{bookingId:guid}/payment")]
    [AllowAnonymous]
    public async Task<IActionResult> InitiatePayment(Guid bookingId)
    {
        var result = await _mediator.Send(new InitiatePaymentCommand(bookingId));
        return Ok(result);
    }
}
