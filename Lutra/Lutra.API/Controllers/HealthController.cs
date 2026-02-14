using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : Controller
{
    public IActionResult Index()
    {
        return Ok();
    }
}
