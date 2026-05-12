using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Features.Auth.Commands.AcceptInvitation;
using ApexBooking.Core.Application.Features.Auth.Commands.AccountVerification;
using ApexBooking.Core.Application.Features.Auth.Commands.ForgotPassword;
using ApexBooking.Core.Application.Features.Auth.Commands.Login;
using ApexBooking.Core.Application.Features.Auth.Commands.LoginSuperAdmin;
using ApexBooking.Core.Application.Features.Auth.Commands.Logout;
using ApexBooking.Core.Application.Features.Auth.Commands.RefreshToken;
using ApexBooking.Core.Application.Features.Auth.Commands.ResetPassword;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

        [HttpGet("verify-account")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAccount(
            string token,
            string? returnTo,
            CancellationToken ct)
        {
            var result = await _mediator.Send(new AccountVerificationCommand(token, returnTo));
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
        {
            var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
            return Ok(result);


        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken ct)
        {
            var result = await _mediator.Send(new ForgotPasswordCommand(request.Email));
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [Authorize]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken ct)
        {
            var result = await _mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword, request.ConfirmPassword));
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var result = await _mediator.Send(new RefreshTokenCommand());
            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _mediator.Send(new LogoutCommand());
            return NoContent();
        }

        [HttpPost("accept-invitation")]
        [AllowAnonymous]
        public async Task<IActionResult> AcceptInvitation(
            [FromBody] AcceptInvitationRequestDto request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new AcceptInvitationCommand(request.Token, request.NewPassword, request.ConfirmPassword), ct);
            return Ok(result);
        }

        [HttpPost("login/superadmin")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginSuperAdmin(
            [FromBody] LoginSuperAdminRequestDto request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(new LoginSuperAdminCommand(request.Email, request.Password), ct);
            return Ok(result);
        }
    }
}