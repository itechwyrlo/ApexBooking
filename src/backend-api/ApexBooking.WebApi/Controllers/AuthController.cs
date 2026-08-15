using ApexBooking.Core.Application.Dtos.Request;
using ApexBooking.Core.Application.Features.Authentication.FindWorkspace;
using ApexBooking.Core.Application.Features.Authentication.GetWorkspaceSummary;
using ApexBooking.Core.Application.Features.Authentication.Login;
using ApexBooking.Core.Application.Features.Authentication.Logout;
using ApexBooking.Core.Application.Features.Authentication.PlatformAdminLogin;
using ApexBooking.Core.Application.Features.Authentication.RefreshToken;
using ApexBooking.Core.Application.Features.Authentication.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApexBooking.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator, ILogger<AuthController> logger)
        {
            _logger = logger;
            _mediator = mediator;
        }

        // Slug is a route segment, not the request body — TenantMiddleware resolves it into the
        // ambient tenant before this action runs (see TenantLoginCommand's doc comment). The
        // absolute route overrides the controller's [Route("api/[controller]")] convention, which
        // is why this doesn't just live at "login" like the rest of this controller's actions.
        [HttpPost("/api/{slug}/auth/login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromRoute] string slug, [FromBody] LoginRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new TenantLoginCommand(request.Email, request.Password), ct);
            return Ok(result);
        }

        // Lets the frontend confirm a workspace is real (and grab its display name/logo) before
        // showing a confident "Welcome back" state for a slug reached via a direct link
        // (/:slug/login) that was never looked up through find-workspace. Same slug-resolution
        // mechanism as Login just above — a bad slug never reaches this action at all, since
        // TenantMiddleware responds 404 itself first.
        [HttpGet("/api/{slug}/workspace-summary")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> GetWorkspaceSummary(CancellationToken ct)
        {
            var result = await _mediator.Send(new GetWorkspaceSummaryQuery(), ct);
            return Ok(result);
        }

        // Separate surface, separate handler, no slug — a platform admin isn't a member of any
        // tenant, so there's nothing to scope this to.
        [HttpPost("/api/superadmin/auth/login")]
        [AllowAnonymous]
        public async Task<IActionResult> PlatformAdminLogin([FromBody] LoginRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new PlatformAdminLoginCommand(request.Email, request.Password), ct);
            return Ok(result);
        }

        // Never throws, never distinguishes "no such email" from "no workspace" — see
        // FindWorkspaceResult's doc comment. Rate-limited on top of the global limiter since this is
        // an email-enumeration-shaped endpoint by nature.
        [HttpPost("find-workspace")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> FindWorkspace([FromBody] FindWorkspaceRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new FindWorkspaceQuery(request.Email), ct);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken ct)
        {
            var result = await _mediator.Send(
                new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword, request.ConfirmPassword), ct);

            return Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(CancellationToken ct)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(IsPlatformAdmin: false), ct);
            return Ok(result);
        }

        // Separate surface, separate cookie — mirrors the login split above so a superadmin
        // session in one tab can never rotate/overwrite a tenant session's refresh cookie
        // in another tab (see RefreshTokenCommand's doc comment).
        [HttpPost("/api/superadmin/auth/refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshSuperAdmin(CancellationToken ct)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(IsPlatformAdmin: true), ct);
            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _mediator.Send(new LogoutCommand());
            return NoContent();
        }
    
    }
}