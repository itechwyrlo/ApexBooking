using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using ApexBooking.Core.Application.Features.Notifications.Queries.GetLatestNotifications;
using ApexBooking.Core.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    // Recipient-scoped by the current user id (IUserContextService.GetCurrentUserId()), not by
    // tenant or platform-admin status — a tenant Owner and a platform admin hit the exact same
    // endpoints and each only ever sees their own Notification rows. See INotificationRepository.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<NotificationSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatest([FromQuery] int limit, CancellationToken ct)
        {
            var query = limit > 0 ? new GetLatestNotificationsQuery(limit) : new GetLatestNotificationsQuery();
            var result = await _mediator.Send(query, ct);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetUnreadNotificationCountQuery(), ct);
            return Ok(result);
        }

        [HttpPost("mark-all-read")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        {
            await _mediator.Send(new MarkAllNotificationsReadCommand(), ct);
            return NoContent();
        }
    }
}
