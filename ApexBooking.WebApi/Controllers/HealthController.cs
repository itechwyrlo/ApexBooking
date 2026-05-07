using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.Infrastructure.Configuration;

namespace ApexBooking.WebApi.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApexBookingDbContext _dbContext;

    public HealthController(IConfiguration configuration, ApexBookingDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var checks = new Dictionary<string, string>();
        var overallStatus = "Healthy";

        try
        {
            await _dbContext.Database.CanConnectAsync();
            checks["database"] = "Healthy";
        }
        catch
        {
            checks["database"] = "Unhealthy";
            overallStatus = "Unhealthy";
        }

        try
        {
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            
            if (!string.IsNullOrEmpty(jwtIssuer) && !string.IsNullOrEmpty(jwtAudience))
            {
                checks["configuration"] = "Healthy";
            }
            else
            {
                checks["configuration"] = "Unhealthy";
                overallStatus = "Unhealthy";
            }
        }
        catch
        {
            checks["configuration"] = "Unhealthy";
            overallStatus = "Unhealthy";
        }

        return Ok(new
        {
            status = overallStatus,
            checks
        });
    }
}
