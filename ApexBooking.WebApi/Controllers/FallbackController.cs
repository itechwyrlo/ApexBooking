using Microsoft.AspNetCore.Mvc;

namespace ApexBooking.WebApi.Controllers
{
    // NO [ApiController] and NO [Route] here.
    // This allows MapFallbackToController to call it manually.
    public class FallbackController : Controller
    {
        public IActionResult Index()
        {
            return PhysicalFile(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "index.html"), 
                "text/html"
            );
        }
    }
}