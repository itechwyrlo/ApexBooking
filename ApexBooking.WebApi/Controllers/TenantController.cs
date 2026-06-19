using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Features.Tenants.Commands.UpdateTenantProfile;
using ApexBooking.Core.Application.Features.Tenants.Queries.GetTenantProfile;
using ApexBooking.WebApi.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ApexBooking.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class TenantController : ControllerBase
    {
        private readonly ILogger<TenantController> _logger;
        private readonly IMediator _mediator;

        public TenantController(IMediator mediator, ILogger<TenantController> logger)
        {
            _mediator = mediator;
        }

        [HttpGet("profile/{slug}")]
        [Authorize()]
        public async Task<IActionResult> GetProfile(string slug)
        {
            var response = await _mediator.Send(new GetTenantProfileQuery(slug));
            return Ok(response);
        }

        [HttpPut("profile/{slug}")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string slug, [FromBody] UpdateProfileRequestDto request)
        {
            await _mediator.Send(new UpdateTenantProfileCommand(
                TenantSlug: slug,
                LogoUrl: request.LogoUrl,
                AddressLine1: request.AddressLine1,
                AddressLine2: request.AddressLine2,
                City: request.City,
                State: request.State,
                PostalCode: request.PostalCode,
                CountryCode: request.CountryCode,
                Timezone: request.Timezone,
                CurrencyCode: request.CurrencyCode,
                WebsiteUrl: request.WebsiteUrl,
                ContactEmail: request.ContactEmail,
                ContactPhone: request.ContactPhone,
                DateFormat: request.DateFormat,
                TimeFormat: request.TimeFormat,
                LanguageCode: request.LanguageCode));

            return NoContent();
        }
    }
}