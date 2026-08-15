using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/public/refund-status")]
    [AllowAnonymous]
    public class RefundStatusController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RefundStatusController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> Get(string token)
        {
            var result = await _mediator.Send(new GetRefundStatusQuery(token));
            return Ok(result);
        }
    }
}
