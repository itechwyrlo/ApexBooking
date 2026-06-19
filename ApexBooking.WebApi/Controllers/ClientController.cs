using ApexBooking.Core.Application.Features.Clients.Queries.GetClientByEmail;
using ApexBooking.Core.Application.Features.Clients.Queries.GetClients;
using ApexBooking.SharedKernel.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] QueryObjectParams param)
    {
        var result = await _mediator.Send(new GetClientsQuery(param));
        return Ok(result);
    }

    [HttpGet("detail")]
    [Authorize]
    public async Task<IActionResult> GetByEmail([FromQuery] string email)
    {
        var result = await _mediator.Send(new GetClientByEmailQuery(email));
        return Ok(result);
    }
}
