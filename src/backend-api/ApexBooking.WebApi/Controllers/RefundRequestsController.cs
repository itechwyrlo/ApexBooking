using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.ConfirmRefundRequest;
using ApexBooking.Core.Application.Features.RefundRequests.Commands.RejectRefundRequest;
using ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests;
using ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog;
using ApexBooking.SharedKernel.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/refund-requests")]
    [Authorize]
    public class RefundRequestsController : ControllerBase
    {
        private const long MaxReceiptSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedReceiptContentTypes = ["image/jpeg", "image/png", "image/webp"];

        private readonly IMediator _mediator;

        public RefundRequestsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Policy = "ManagementOnly")]
        [ProducesResponseType(typeof(QueryResult<RefundRequestSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPending([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _mediator.Send(new GetPendingRefundRequestsQuery(pageNumber, pageSize));
            return Ok(result);
        }

        [HttpGet("log")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(typeof(IReadOnlyCollection<RefundLogEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLog([FromQuery] int limit = 20)
        {
            var result = await _mediator.Send(new GetRefundLogQuery(limit));
            return Ok(result);
        }

        [HttpPost("{id:guid}/confirm")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Confirm(Guid id, [FromForm] IFormFile receipt)
        {
            if (receipt is null || receipt.Length == 0)
                return Problem(title: "Validation Error", detail: "A receipt image is required.", statusCode: StatusCodes.Status400BadRequest);

            if (receipt.Length > MaxReceiptSizeBytes)
                return Problem(title: "Validation Error", detail: "Receipt must be 5MB or smaller.", statusCode: StatusCodes.Status400BadRequest);

            if (Array.IndexOf(AllowedReceiptContentTypes, receipt.ContentType) < 0)
                return Problem(title: "Validation Error", detail: "Receipt must be a JPEG, PNG, or WebP image.", statusCode: StatusCodes.Status400BadRequest);

            var extension = receipt.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg",
            };

            await using var stream = receipt.OpenReadStream();
            await _mediator.Send(new ConfirmRefundRequestCommand(id, stream, receipt.ContentType, extension));
            return NoContent();
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Policy = "ManagementOnly")]
        public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRefundRequestBody body)
        {
            await _mediator.Send(new RejectRefundRequestCommand(id, body.Reason));
            return NoContent();
        }
    }

    public record RejectRefundRequestBody(string Reason);
}
