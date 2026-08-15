using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Features.Platform.Commands.RetryOutboxMessage;
using ApexBooking.Core.Application.Features.Platform.Queries.GetDashboardSummary;
using ApexBooking.Core.Application.Features.Platform.Queries.GetFailedOutboxMessages;
using ApexBooking.Core.Application.Features.Platform.Queries.GetPlatformBookings;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/superadmin")]
    [Produces("application/json")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SuperAdminController(IMediator mediator) => _mediator = mediator;

        [HttpGet("dashboard-summary")]
        [ProducesResponseType(typeof(PlatformDashboardSummary), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
        {
            var response = await _mediator.Send(new GetPlatformDashboardSummaryQuery(), ct);
            return Ok(response);
        }

        [HttpGet("bookings")]
        [ProducesResponseType(typeof(QueryResult<PlatformBookingSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBookings(
            [FromQuery] QueryObjectParams param,
            [FromQuery] Guid? tenantId,
            [FromQuery] BookingStatus? status,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate,
            CancellationToken ct)
        {
            var response = await _mediator.Send(new GetPlatformBookingsQuery(param, tenantId, status, fromDate, toDate), ct);
            return Ok(response);
        }

        // Failed notification/email deliveries — see the "Hangfire + Outbox Core" design spec's
        // Status=Failed/LastError/RetryCount data shape, produced by IOutboxRelayService.
        [HttpGet("outbox-messages/failed")]
        [ProducesResponseType(typeof(IReadOnlyCollection<FailedOutboxMessageSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFailedOutboxMessages(CancellationToken ct)
        {
            var response = await _mediator.Send(new GetFailedOutboxMessagesQuery(), ct);
            return Ok(response);
        }

        [HttpPost("outbox-messages/{id:guid}/retry")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RetryOutboxMessage(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new RetryOutboxMessageCommand(id), ct);
            return NoContent();
        }
    }
}
