using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization; // 🌟 Required for security policy decoration
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.SharedKernel.Models;
using ApexBooking.Core.Application.Features.Tenancy.Commands.StaffBreaks;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetStaffBreaks;
using ApexBooking.Core.Application.Features.Tenancy.Commands.WeeklySchedule;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllTeam;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetTeamMemberSchedule;
using ApexBooking.Core.Application.Features.Tenancy.Commands.AddTeam;
using ApexBooking.Core.Application.Features.Tenancy.Commands.UpdateTeamMember;
using ApexBooking.Core.Application.Features.Tenancy.Commands.RemoveTeamMember;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetTeamMemberRemovalImpact;
using ApexBooking.Core.Application.Features.Services.Queries.GetAllServices;
using ApexBooking.Core.Application.Features.Services.Queries.GetServicesByBranch;
using ApexBooking.Core.Application.Features.Services.Commands.CreateService;
using ApexBooking.Core.Application.Features.Services.Commands.UpdateService;
using ApexBooking.Core.Application.Features.Services.Commands.AssignStaffToService;
using ApexBooking.Core.Application.Features.Services.Commands.UnassignStaffFromService;
using ApexBooking.Core.Application.Features.Services.Queries.GetServiceStaff;
using ApexBooking.Core.Application.Features.Tenancy.Commands.BookingPolicy;
using ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentPolicy;
using ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentGateway;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentGatewayCredential;
using ApexBooking.Core.Application.Features.Tenancy.Commands.BusinessProfile;
using ApexBooking.Core.Application.Features.Tenancy.Commands.Appearance;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetAppearance;
using ApexBooking.Core.Application.Features.Tenancy.Commands.ScanArrival;
using ApexBooking.Core.Application.Features.Tenancy.Commands.Branches;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllBranches;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetBranch;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetBusinessProfile;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetBookingPolicy;
using ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentPolicy;
using ApexBooking.Core.Application.Features.Bookings.Commands.CompleteBooking;
using ApexBooking.Core.Application.Features.Bookings.Commands.SetBookingStaffNotes;
using ApexBooking.Core.Application.Features.Bookings.Commands.ReassignBooking;
using ApexBooking.Core.Application.Features.Bookings.Commands.RecordPayInVisitPayment;
using ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking;
using ApexBooking.Core.Application.Features.Bookings.Commands.MarkBookingNoShow;
using ApexBooking.Core.Application.Features.Bookings.Commands.ScheduleBooking;
using ApexBooking.Core.Application.Features.Bookings.Commands.AdmitBooking;
using ApexBooking.Core.Application.Features.Bookings.Commands.CheckoutBooking;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetCheckoutDetails;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookings;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookingCounts;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantRevenue;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetReassignableStaff;
using ApexBooking.Core.Application.Features.Bookings.Queries.GetWalkInAvailableStaff;
using ApexBooking.Core.Application.Features.Staff.Queries.GetIdleStaff;
using ApexBooking.Core.Application.Features.Staff.Queries.GetStaffPerformance;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Application.Features.Customers.Queries.GetAllCustomers;
using ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerBookings;
using ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerLatestNote;
using ApexBooking.Core.Application.Features.Customers.Queries.SearchCustomers;
using ApexBooking.Core.Application.Features.TimeOffs.Commands.RequestTimeOff;
using ApexBooking.Core.Application.Features.TimeOffs.Commands.BlockMyTime;
using ApexBooking.Core.Application.Features.TimeOffs.Commands.ApproveTimeOff;
using ApexBooking.Core.Application.Features.TimeOffs.Commands.RejectTimeOff;
using ApexBooking.Core.Application.Features.TimeOffs.Queries.GetTimeOffRequests;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManagementOnly")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public class TenantController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TenantController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ── Task 1: Team Roster Provisioning Endpoints ───────────────────────

        [HttpPost("add-team")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddTeam([FromBody] AddTeamCommand command)
        {
            await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpGet("team")]
        [ProducesResponseType(typeof(QueryResult<TeamMemberSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTeam([FromQuery] QueryObjectParams param)
        {
            var result = await _mediator.Send(new GetAllTeamQuery(param));
            return Ok(result);
        }

        [HttpGet("team/idle")]
        [ProducesResponseType(typeof(IReadOnlyCollection<IdleStaffDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIdleStaff()
        {
            var result = await _mediator.Send(new GetIdleStaffQuery());
            return Ok(result);
        }

        [HttpGet("team/performance")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StaffPerformanceEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStaffPerformance([FromQuery] DateOnly date)
        {
            var result = await _mediator.Send(new GetStaffPerformanceQuery(date));
            return Ok(result);
        }

        [HttpPut("team/{tenantMemberId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateTeamMember([FromRoute] Guid tenantMemberId, [FromBody] UpdateTeamMemberCommand command)
        {
            await _mediator.Send(command with { TenantMemberId = tenantMemberId });
            return NoContent();
        }

        [HttpGet("team/{tenantMemberId:guid}/removal-impact")]
        [ProducesResponseType(typeof(TeamMemberRemovalImpact), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTeamMemberRemovalImpact([FromRoute] Guid tenantMemberId)
        {
            var result = await _mediator.Send(new GetTeamMemberRemovalImpactQuery(tenantMemberId));
            return Ok(result);
        }

        [HttpDelete("team/{tenantMemberId:guid}")]
        [ProducesResponseType(typeof(TeamMemberRemovalResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveTeamMember([FromRoute] Guid tenantMemberId)
        {
            var result = await _mediator.Send(new RemoveTeamMemberCommand(tenantMemberId));
            return Ok(result);
        }

        // ── Task 2: Schedule & Availability Management Endpoints ───────────────

        [HttpGet("team/{tenantMemberId:guid}/schedule")]
        [ProducesResponseType(typeof(IReadOnlyCollection<DayScheduleSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTeamMemberSchedule([FromRoute] Guid tenantMemberId)
        {
            var result = await _mediator.Send(new GetTeamMemberScheduleQuery(tenantMemberId));
            return Ok(result);
        }

        [HttpPut("team/schedule")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateWeeklySchedule([FromBody] UpdateWeeklyScheduleCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpGet("team/{tenantMemberId:guid}/breaks")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StaffBreakSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetStaffBreaks([FromRoute] Guid tenantMemberId)
        {
            var result = await _mediator.Send(new GetStaffBreaksQuery(tenantMemberId));
            return Ok(result);
        }

        [HttpPost("team/break")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // Returns { id: Guid }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddStaffBreak([FromBody] AddStaffBreakCommand command)
        {
            var breakId = await _mediator.Send(command);
            return Ok(new { id = breakId });
        }

        [HttpDelete("team/{tenantMemberId:guid}/break/{breakId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveStaffBreak([FromRoute] Guid tenantMemberId, [FromRoute] Guid breakId)
        {
            await _mediator.Send(new RemoveStaffBreakCommand(tenantMemberId, breakId));
            return NoContent();
        }

        // ── Team Time-Off Requests ───────────────────────────────────────────────
        // GET/POST time-off override the class-level ManagementOnly policy so every role can
        // submit and review their own requests; approve/reject are Owner-only.

        [HttpGet("team/time-off")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(QueryResult<TimeOffRequestSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTimeOffRequests([FromQuery] QueryObjectParams param, [FromQuery] TimeOffStatus? status)
        {
            var result = await _mediator.Send(new GetTimeOffRequestsQuery(param, status));
            return Ok(result);
        }

        [HttpPost("team/time-off")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)] // Returns { id: Guid }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestTimeOff([FromBody] RequestTimeOffCommand command)
        {
            var requestId = await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, new { id = requestId });
        }

        [HttpPost("team/time-off/block")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)] // Returns { id: Guid }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BlockMyTime([FromBody] BlockMyTimeCommand command)
        {
            var requestId = await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, new { id = requestId });
        }

        [HttpPost("team/time-off/{timeOffRequestId:guid}/approve")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApproveTimeOff([FromRoute] Guid timeOffRequestId)
        {
            await _mediator.Send(new ApproveTimeOffCommand(timeOffRequestId));
            return NoContent();
        }

        [HttpPost("team/time-off/{timeOffRequestId:guid}/reject")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RejectTimeOff([FromRoute] Guid timeOffRequestId)
        {
            await _mediator.Send(new RejectTimeOffCommand(timeOffRequestId));
            return NoContent();
        }

        // ── Task 3: Service Catalog Management Endpoints ──────────────────────

        [HttpPost("services")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)] // Returns { id: Guid }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceCommand command)
        {
            var serviceId = await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, new { id = serviceId });
        }

        [HttpPut("services")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateService([FromBody] UpdateServiceCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpGet("services")]
        [ProducesResponseType(typeof(QueryResult<ServiceCatalogSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetServices([FromQuery] QueryObjectParams param)
        {
            var result = await _mediator.Send(new GetAllServiceQuery(param));
            return Ok(result);
        }

        // Backs the walk-in flow's service picker — only services at least one active staff member
        // at this branch can actually perform. Staff included since walk-in creation itself is.
        [HttpGet("branches/{branchId:guid}/services")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(IReadOnlyCollection<ServiceCatalogSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetServicesByBranch([FromRoute] Guid branchId)
        {
            var result = await _mediator.Send(new GetServicesByBranchQuery(branchId));
            return Ok(result);
        }

        [HttpGet("services/{serviceId:guid}/staff")]
        [ProducesResponseType(typeof(IReadOnlyCollection<StaffAssignmentSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetServiceStaff([FromRoute] Guid serviceId)
        {
            var result = await _mediator.Send(new GetServiceStaffQuery(serviceId));
            return Ok(result);
        }

        [HttpPost("services/{serviceId:guid}/staff")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignStaffToService([FromRoute] Guid serviceId, [FromBody] AssignStaffToServiceCommand command)
        {
            await _mediator.Send(command with { ServiceId = serviceId });
            return NoContent();
        }

        [HttpDelete("services/{serviceId:guid}/staff/{tenantMemberId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnassignStaffFromService([FromRoute] Guid serviceId, [FromRoute] Guid tenantMemberId)
        {
            await _mediator.Send(new UnassignStaffFromServiceCommand(serviceId, tenantMemberId));
            return NoContent();
        }

        // ── Client Management Endpoints ─────────────────────────────────────────

        [HttpGet("customers")]
        [ProducesResponseType(typeof(QueryResult<CustomerSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomers([FromQuery] QueryObjectParams param)
        {
            var result = await _mediator.Send(new GetAllCustomersQuery(param));
            return Ok(result);
        }

        // Backs the walk-in flow's "find an existing customer" search — Staff included since
        // walk-in creation itself is Staff-eligible (see bookings/walk-in-staff below).
        [HttpGet("customers/search")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(IReadOnlyCollection<CustomerSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchCustomers([FromQuery] string term)
        {
            var result = await _mediator.Send(new SearchCustomersQuery(term));
            return Ok(result);
        }

        [HttpGet("customers/{customerId:guid}/bookings")]
        [ProducesResponseType(typeof(QueryResult<CustomerBookingSummary>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCustomerBookings([FromRoute] Guid customerId, [FromQuery] QueryObjectParams param)
        {
            var result = await _mediator.Send(new GetCustomerBookingsQuery(customerId, param));
            return Ok(result);
        }

        // Staff included — this backs the Staff Dashboard's own "active appointment" note preview.
        [HttpGet("customers/{customerId:guid}/latest-note")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(CustomerLatestNoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetCustomerLatestNote([FromRoute] Guid customerId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCustomerLatestNoteQuery(customerId), cancellationToken);
            return result is null ? NoContent() : Ok(result);
        }

        [HttpGet("profile")]
        [ProducesResponseType(typeof(BusinessProfileDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBusinessProfile()
        {
            var result = await _mediator.Send(new GetBusinessProfileQuery());
            return Ok(result);
        }

        [HttpPut("profile")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBusinessProfile([FromBody] UpdateBusinessProfileCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpGet("appearance")]
        [ProducesResponseType(typeof(AppearanceDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAppearance()
        {
            var result = await _mediator.Send(new GetAppearanceQuery());
            return Ok(result);
        }

        [HttpPut("appearance")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAppearance([FromBody] UpdateAppearanceCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        // ── Branch Management Endpoints ────────────────────────────────────────

        [HttpGet("branches")]
        [ProducesResponseType(typeof(IReadOnlyCollection<BranchAdminSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranches()
        {
            var result = await _mediator.Send(new GetAllBranchesQuery());
            return Ok(result);
        }

        [HttpGet("branches/{branchId:guid}")]
        [ProducesResponseType(typeof(BranchDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetBranch([FromRoute] Guid branchId)
        {
            var result = await _mediator.Send(new GetBranchQuery(branchId));
            return Ok(result);
        }

        [HttpPost("branches")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)] // Returns { id: Guid }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddBranch([FromBody] AddBranchCommand command)
        {
            var branchId = await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, new { id = branchId });
        }

        [HttpPut("branches/{branchId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBranchProfile([FromRoute] Guid branchId, [FromBody] UpdateBranchProfileCommand command)
        {
            await _mediator.Send(command with { BranchId = branchId });
            return NoContent();
        }

        [HttpPut("branches/{branchId:guid}/hours")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBranchOperatingHours([FromRoute] Guid branchId, [FromBody] UpdateBranchOperatingHoursCommand command)
        {
            await _mediator.Send(command with { BranchId = branchId });
            return NoContent();
        }

        [HttpGet("policy/booking")]
        [ProducesResponseType(typeof(BookingPolicyDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBookingPolicy()
        {
            var result = await _mediator.Send(new GetBookingPolicyQuery());
            return Ok(result);
        }

        [HttpPut("policy/booking")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateBookingPolicy([FromBody] UpdateBookingPolicyCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpGet("policy/payment")]
        [ProducesResponseType(typeof(PaymentPolicyDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentPolicy()
        {
            var result = await _mediator.Send(new GetPaymentPolicyQuery());
            return Ok(result);
        }

        [HttpPut("policy/payment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePaymentPolicy([FromBody] UpdatePaymentPolicyCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        // ── Payment Gateway (PayMongo) Credentials — Owner-only ─────────────────

        [HttpGet("payment-gateway")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(typeof(PaymentGatewayCredentialDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentGatewayCredential()
        {
            var result = await _mediator.Send(new GetPaymentGatewayCredentialQuery());
            return Ok(result);
        }

        [HttpPut("payment-gateway")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfigurePaymentGateway([FromBody] ConfigurePaymentGatewayCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPut("payment-gateway/webhook-secret")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetPaymentGatewayWebhookSecret([FromBody] SetPaymentGatewayWebhookSecretCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        // ── Appointments / Bookings Management Endpoints ────────────────────────

        [HttpGet("bookings")]
        [ProducesResponseType(typeof(QueryResult<TenantBookingSummary>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBookings(
            [FromQuery] QueryObjectParams param,
            [FromQuery] Guid? branchId,
            [FromQuery] Guid? staffId,
            [FromQuery] BookingStatus? status,
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate)
        {
            var result = await _mediator.Send(new GetTenantBookingsQuery(param, branchId, staffId, status, fromDate, toDate));
            return Ok(result);
        }

        [HttpGet("bookings/counts")]
        [ProducesResponseType(typeof(TenantBookingCountsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBookingCounts([FromQuery] DateOnly date)
        {
            var result = await _mediator.Send(new GetTenantBookingCountsQuery(date));
            return Ok(result);
        }

        [HttpGet("bookings/revenue")]
        [Authorize(Roles = "Owner")]
        [ProducesResponseType(typeof(TenantRevenueDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenue([FromQuery] DateOnly date)
        {
            var result = await _mediator.Send(new GetTenantRevenueQuery(date));
            return Ok(result);
        }

        [HttpGet("bookings/{bookingId:guid}/reassignable-staff")]
        [ProducesResponseType(typeof(IReadOnlyCollection<ReassignableStaffDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReassignableStaff([FromRoute] Guid bookingId)
        {
            var result = await _mediator.Send(new GetReassignableStaffQuery(bookingId));
            return Ok(result);
        }

        [HttpGet("bookings/walk-in-staff")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(IReadOnlyCollection<WalkInStaffAvailability>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetWalkInAvailableStaff([FromQuery] Guid branchId, [FromQuery] Guid serviceId)
        {
            var result = await _mediator.Send(new GetWalkInAvailableStaffQuery(branchId, serviceId));
            return Ok(result);
        }

        [HttpPost("bookings")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)] // Returns { id: Guid }
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ScheduleBooking([FromBody] ScheduleBookingCommand command)
        {
            var bookingId = await _mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, new { id = bookingId });
        }

        // ── Secure QR Boarding Pass — mobile scanner check-in ──────────────────

        [HttpPost("bookings/scan-arrival")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(ScanArrivalResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ScanArrival([FromBody] ScanArrivalCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("bookings/{bookingId:guid}/admit")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(ScanArrivalResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AdmitBooking([FromRoute] Guid bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new AdmitBookingCommand(bookingId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("bookings/{bookingId:guid}/checkout-detail")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(CheckoutDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCheckoutDetail([FromRoute] Guid bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCheckoutDetailsQuery(bookingId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("bookings/{bookingId:guid}/checkout")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(typeof(CheckoutBookingResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckoutBooking([FromRoute] Guid bookingId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CheckoutBookingCommand(bookingId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("bookings/{bookingId:guid}/complete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteBooking([FromRoute] Guid bookingId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new CompleteBookingCommand(bookingId), cancellationToken);
            return NoContent();
        }

        [HttpPost("bookings/{bookingId:guid}/reassign")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReassignBooking([FromRoute] Guid bookingId, [FromBody] ReassignBookingCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { BookingId = bookingId }, cancellationToken);
            return NoContent();
        }

        [HttpPost("bookings/{bookingId:guid}/collect-payment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CollectPayment([FromRoute] Guid bookingId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new RecordPayInVisitPaymentCommand(bookingId), cancellationToken);
            return NoContent();
        }

        [HttpPost("bookings/{bookingId:guid}/staff-notes")]
        [Authorize(Roles = "Owner,Admin,Staff")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetBookingStaffNotes([FromRoute] Guid bookingId, [FromBody] SetBookingStaffNotesCommand command, CancellationToken cancellationToken)
        {
            await _mediator.Send(command with { BookingId = bookingId }, cancellationToken);
            return NoContent();
        }

        [HttpPost("bookings/{bookingId:guid}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelBooking([FromRoute] Guid bookingId, [FromBody] CancelBookingCommand command)
        {
            await _mediator.Send(command with { BookingId = bookingId });
            return NoContent();
        }

        [HttpPost("bookings/{bookingId:guid}/no-show")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MarkBookingNoShow([FromRoute] Guid bookingId)
        {
            await _mediator.Send(new MarkBookingNoShowCommand(bookingId));
            return NoContent();
        }
    }
}
