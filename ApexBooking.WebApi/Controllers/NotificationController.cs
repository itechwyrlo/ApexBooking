using ApexBooking.Core.Application.Features.Notifications.Commands.MarkAllRead;
using ApexBooking.Core.Application.Features.Notifications.Commands.RegisterFcmToken;
using ApexBooking.Core.Application.Features.Notifications.Commands.UnregisterFcmToken;
using ApexBooking.Core.Application.Features.Notifications.Queries.GetNotifications;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetNotifications()
    {
        var result = await _mediator.Send(new GetNotificationsQuery());
        return Ok(result);
    }

    [HttpPatch("read-all")]
    [Authorize]
    public async Task<IActionResult> MarkAllRead()
    {
        await _mediator.Send(new MarkAllReadCommand());
        return NoContent();
    }

    [HttpPost("register-token")]
    [Authorize]
    public async Task<IActionResult> RegisterToken([FromBody] FcmTokenRequestDto dto)
    {
        await _mediator.Send(new RegisterFcmTokenCommand(dto.Token));
        return NoContent();
    }

    [HttpDelete("unregister-token")]
    [Authorize]
    public async Task<IActionResult> UnregisterToken([FromBody] FcmTokenRequestDto dto)
    {
        await _mediator.Send(new UnregisterFcmTokenCommand(dto.Token));
        return NoContent();
    }
}
