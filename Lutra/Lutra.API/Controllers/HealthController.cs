using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Controllers;

/// <summary>
/// Exposes a lightweight health check for the API.
/// </summary>
[ApiController]
[Route("health")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Returns the API health status.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return Ok();
    }
}