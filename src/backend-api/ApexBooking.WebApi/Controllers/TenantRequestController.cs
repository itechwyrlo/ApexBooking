using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ApexBooking.Core.Application.Dtos.Request;
using ApexBooking.Core.Application.Dtos.Response; // Where PendingTenantRequestSummary resides
using ApexBooking.Core.Application.Features.TenantRequest.Commands.Approve;
using ApexBooking.Core.Application.Features.TenantRequest.Commands.Reject;
using ApexBooking.Core.Application.Features.TenantRequest.Commands.RequestReceived;
using ApexBooking.Core.Application.Features.TenantRequest.Queries.GetAllRequest;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TenantRequestsController : ControllerBase
    {
        private readonly ILogger<TenantRequestsController> _logger;
        private readonly IMediator _mediator;

        public TenantRequestsController(ILogger<TenantRequestsController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpPost("request-access")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestAccess([FromBody] RequestAccessDto request, CancellationToken ct)
        {
            await _mediator.Send(
                new RequestReceivedCommand(
                    request.BusinessName,
                    request.BusinessType,
                    request.Slug,
                    request.OwnerFirstName,
                    request.OwnerLastName,
                    request.OwnerEmail,
                    request.RequestedPlan), ct);

            return Accepted();
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(typeof(QueryResult<PendingTenantRequestSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] QueryObjectParams param)
        {
            var response = await _mediator.Send(new GetAllPendingRequestQuery(param));
            return Ok(response);
        }

        [HttpPost("approved-request/{requestId:guid}")]
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApprovedRequest([FromRoute] Guid requestId, CancellationToken ct)
        {
            await _mediator.Send(new ApprovedTenantRequestCommand(requestId), ct);
            return NoContent();
        }

        [HttpPost("reject-request/{requestId:guid}")]
        [Authorize(Policy = "SuperAdminOnly")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectRequest(
            [FromRoute] Guid requestId,
            [FromBody] RejectTenantRequest request,
            CancellationToken ct)
        {
            await _mediator.Send(new RejectTenantRequestCommand(requestId, request.Reason), ct);
            return NoContent();
        }
    }
}
