using ApexBooking.Core.Application.Features.SuperAdmin.Commands.ApproveTenantRequest;
using ApexBooking.Core.Application.Features.SuperAdmin.Commands.AssignExistingUser;
using ApexBooking.Core.Application.Features.SuperAdmin.Commands.ResendInvitation;
using ApexBooking.Core.Application.Features.SuperAdmin.Commands.ConfigurePlatformPaymentGateway;
using ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateOrganization;
using ApexBooking.Core.Application.Features.SuperAdmin.Commands.CreateTenantUser;
using ApexBooking.Core.Application.Features.SuperAdmin.Commands.RejectTenantRequest;
using ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetOrganizationDetail;
using ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformOverview;
using ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformPaymentGateway;
using ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequestById;
using ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequests;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SuperAdminOnly")]
public class SuperAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuperAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPlatformOverviewQuery(), ct);
        return Ok(result);
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPlatformOverviewQuery(), ct);
        return Ok(result);
    }

    [HttpPost("organizations")]
    public async Task<IActionResult> CreateOrganization(
        [FromBody] CreateOrganizationRequestDto request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateOrganizationCommand(
            request.Slug,
            request.BusinessName,
            request.OwnerFullName,
            request.OwnerEmail,
            request.OwnerPhone,
            request.AdminPassword), ct);

        return Ok(result);
    }

    [HttpGet("organizations/{slug}")]
    public async Task<IActionResult> GetOrganizationDetail(string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrganizationDetailQuery(slug), ct);
        return Ok(result);
    }

    [HttpPost("organizations/{slug}/users")]
    public async Task<IActionResult> CreateTenantUser(
        string slug,
        [FromBody] CreateTenantUserRequestDto request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateTenantUserCommand(
            slug,
            request.FullName,
            request.Email,
            request.Role), ct);

        return Ok(result);
    }

    [HttpPost("organizations/{slug}/users/assign")]
    public async Task<IActionResult> AssignExistingUser(
        string slug,
        [FromBody] AssignExistingUserRequestDto request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignExistingUserCommand(
            slug,
            request.Email,
            request.Role), ct);

        return Ok(result);
    }

    [HttpPost("organizations/{slug}/users/{userId:guid}/resend-invite")]
    public async Task<IActionResult> ResendInvitation(
        string slug,
        Guid userId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ResendInvitationCommand(slug, userId), ct);
        return Ok(result);
    }

    [HttpGet("payment-gateway")]
    public async Task<IActionResult> GetPaymentGateway(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPlatformPaymentGatewayQuery(), ct);
        return Ok(result);
    }

    [HttpPost("payment-gateway")]
    public async Task<IActionResult> ConfigurePaymentGateway(
        [FromBody] ConfigurePlatformPaymentGatewayRequestDto request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfigurePlatformPaymentGatewayCommand(
            request.GatewayProvider,
            request.ClientId,
            request.SecretKey,
            request.WebhookId,
            request.Mode), ct);

        return Ok(result);
    }

    [HttpGet("tenant-requests")]
    public async Task<IActionResult> GetTenantRequests([FromQuery] string? status, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTenantRequestsQuery(status), ct);
        return Ok(result);
    }

    [HttpGet("tenant-requests/{id:guid}")]
    public async Task<IActionResult> GetTenantRequestById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTenantRequestByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("tenant-requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveTenantRequest(
        Guid id,
        [FromBody] ApproveTenantRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ApproveTenantRequestCommand(id, dto.Slug, dto.TrialDays), ct);
        return Ok(result);
    }

    [HttpPost("tenant-requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectTenantRequest(
        Guid id,
        [FromBody] RejectTenantRequestDto dto,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new RejectTenantRequestCommand(id, dto.Reason), ct);
        return Ok(result);
    }
}
