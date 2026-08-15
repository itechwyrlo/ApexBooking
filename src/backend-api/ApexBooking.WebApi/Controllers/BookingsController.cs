using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Features.Bookings.Commands.InitiateBooking;
using ApexBooking.Core.Application.Features.Bookings.Queries.BookableStaffs;
using ApexBooking.Core.Application.Features.Bookings.Queries.Slots;
using ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicBranches;
using ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicServicesByBranch;
using ApexBooking.Core.Application.Features.PublicBookings.Queries.GetBookingStatusByTicket;
using ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicTheme;
using ApexBooking.Core.Application.Features.PublicBookings.Queries.GetBookingQrImage;
using ApexBooking.Core.Application.Features.PublicBookings.Queries.GetCancellableBooking;
using ApexBooking.Core.Application.Features.PublicBookings.Commands.CancelBookingByToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/public/{slug}/bookings")]
    [Produces("application/json")]
    public class BookingsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BookingsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Resolves just enough branding (palette id + dark/light mode) to paint the page's
        /// colors correctly before anything else loads — deliberately separate from any
        /// future business-name/logo/banner endpoint. Absolute route since it sits alongside
        /// "bookings", not nested under it.
        /// </summary>
        [HttpGet("/api/public/{slug}/theme")]
        [ProducesResponseType(typeof(PublicThemeDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTheme(CancellationToken cancellationToken)
        {
            var theme = await _mediator.Send(new GetPublicThemeQuery(), cancellationToken);
            return Ok(theme);
        }

        /// <summary>
        /// STEP 1: Publicly lists the tenant's active physical branches.
        /// </summary>
        [HttpGet("branches")]
        [ProducesResponseType(typeof(IReadOnlyCollection<BranchSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranches(CancellationToken cancellationToken)
        {
            var branches = await _mediator.Send(new GetPublicBranchesQuery(), cancellationToken);
            return Ok(branches);
        }

        /// <summary>
        /// STEP 2: Publicly lists the service catalog offered at the selected branch.
        /// </summary>
        [HttpGet("branches/{branchId:guid}/services")]
        [ProducesResponseType(typeof(IReadOnlyCollection<PublicServiceSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetServices([FromRoute] Guid branchId, CancellationToken cancellationToken)
        {
            var services = await _mediator.Send(new GetPublicServicesByBranchQuery(branchId), cancellationToken);
            return Ok(services);
        }

        /// <summary>
        /// STEP 3: Publicly lists the qualified staff deployed to the branch for the selected service.
        /// </summary>
        [HttpGet("branches/{branchId:guid}/services/{serviceId:guid}/staff")]
        [ProducesResponseType(typeof(IReadOnlyCollection<BookableStaffSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStaff([FromRoute] Guid branchId, [FromRoute] Guid serviceId, CancellationToken cancellationToken)
        {
            var staff = await _mediator.Send(new GetBookableStaffQuery(branchId, serviceId), cancellationToken);
            return Ok(staff);
        }

        /// <summary>
        /// STEP 4: Publicly calculates available time slots for the branch based on store hours, shifts, breaks, and existing bookings.
        /// </summary>
        [HttpGet("branches/{branchId:guid}/availability")]
        [ProducesResponseType(typeof(AvailableSlotsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableSlots(
            [FromRoute] Guid branchId,
            [FromQuery] Guid staffId,
            [FromQuery] Guid serviceId,
            [FromQuery] DateOnly date,
            CancellationToken cancellationToken)
        {
            var slots = await _mediator.Send(new GetAvailableSlotsQuery(branchId, staffId, serviceId, date), cancellationToken);
            return Ok(slots);
        }

        /// <summary>
        /// STEP 5 (FINAL CONFIRMATION): Initiates the customer booking. Evaluates policies and fires up PayMongo QR generation if required.
        /// </summary>
        [HttpPost("initiate")]
        [ProducesResponseType(typeof(BookingInitiationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Initiate([FromBody] InitiateBookingCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// STEP 6 (POST-CONFIRMATION POLL): Reports the current status of a booking from its signed
        /// ticket token — used to detect when an upfront payment has cleared.
        /// </summary>
        [HttpGet("/api/public/bookings/status/{ticketToken}")]
        [ProducesResponseType(typeof(PublicBookingStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetBookingStatus([FromRoute] string ticketToken, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetBookingStatusByTicketQuery(ticketToken), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// The boarding-pass QR as a real hosted image, fetched directly by whatever renders the
        /// confirmation email — Brevo's transactional email API has no inline/CID image support,
        /// and embedding it as a data: URI (fine for the in-browser success screen) gets silently
        /// stripped by most email clients. This is the one delivery path that works everywhere.
        /// </summary>
        [HttpGet("/api/public/bookings/qr/{ticketToken}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetQrImage([FromRoute] string ticketToken, CancellationToken cancellationToken)
        {
            var png = await _mediator.Send(new GetBookingQrImageQuery(ticketToken), cancellationToken);
            return File(png, "image/png");
        }

        /// <summary>
        /// Preview for the emailed "Cancel this booking" link — booking details + whether it's
        /// still cancellable, so the page can show the right state before the customer commits
        /// to anything. Absolute route, un-prefixed by slug: tenant resolves from the token
        /// itself, a wholly separate credential from the admission ticket token above.
        /// </summary>
        [HttpGet("/api/public/bookings/cancel/{token}")]
        [ProducesResponseType(typeof(CancellableBookingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCancellableBooking([FromRoute] string token, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCancellableBookingQuery(token), cancellationToken);
            return Ok(result);
        }

        [HttpPost("/api/public/bookings/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelBooking([FromBody] CancelBookingByTokenCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }
}
