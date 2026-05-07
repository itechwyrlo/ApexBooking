using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Features.Auth.Commands.AccountVerification;
using ApexBooking.Core.Application.Features.Auth.Commands.ForgotPassword;
using ApexBooking.Core.Application.Features.Auth.Commands.Login;
using ApexBooking.Core.Application.Features.Auth.Commands.Logout;
using ApexBooking.Core.Application.Features.Auth.Commands.RefreshToken;
using ApexBooking.Core.Application.Features.Auth.Commands.Register;
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

        [HttpPost("register/admin")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
             [FromBody] RegisterAdminRequestDto request, CancellationToken ct)
        {
            var result = await _mediator.Send(new RegisterAdminCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                request.OrganizationName,
                request.Industry,
                request.Phone,
                request.Country));

            return Ok(result);
        }
        [HttpGet("verify-account")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAccount(string token, CancellationToken ct)
        {
            var result = await _mediator.Send(new AccountVerificationCommand(token));
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
        public async Task<IActionResult> Logout()
        {
            await _mediator.Send(new LogoutCommand());
            return NoContent();
        }
    }
}