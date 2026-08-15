using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApexBooking.Core.Application.Common.ReferenceData.Psgc;

namespace ApexBooking.WebApi.Controllers
{
    // Static PSA reference data (provinces / cities / barangays) — a plain passthrough over
    // IPsgcReferenceService's in-memory lookups, not a tenant-scoped domain query, so it
    // deliberately skips the MediatR command/query pipeline used elsewhere in this API.
    [ApiController]
    [Route("api/reference/psgc")]
    [Authorize]
    [Produces("application/json")]
    public class PsgcController : ControllerBase
    {
        private readonly IPsgcReferenceService _psgcReferenceService;

        public PsgcController(IPsgcReferenceService psgcReferenceService)
        {
            _psgcReferenceService = psgcReferenceService;
        }

        [HttpGet("provinces")]
        [ProducesResponseType(typeof(IReadOnlyCollection<PsgcProvince>), StatusCodes.Status200OK)]
        public IActionResult GetProvinces()
        {
            return Ok(_psgcReferenceService.GetProvinces());
        }

        [HttpGet("provinces/{provCode}/cities")]
        [ProducesResponseType(typeof(IReadOnlyCollection<PsgcCity>), StatusCodes.Status200OK)]
        public IActionResult GetCitiesByProvince([FromRoute] string provCode)
        {
            return Ok(_psgcReferenceService.GetCitiesByProvince(provCode));
        }

        [HttpGet("cities/{munCityCode}/barangays")]
        [ProducesResponseType(typeof(IReadOnlyCollection<PsgcBarangay>), StatusCodes.Status200OK)]
        public IActionResult GetBarangaysByCity([FromRoute] string munCityCode)
        {
            return Ok(_psgcReferenceService.GetBarangaysByCity(munCityCode));
        }
    }
}
