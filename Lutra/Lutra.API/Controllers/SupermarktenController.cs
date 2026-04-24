using Cortex.Mediator;
using Lutra.Application.Supermarkten;
using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Controllers;

/// <summary>
/// Provides a dedicated endpoint group for Supermarkt-related operations.
/// </summary>
/// <remarks>
/// This controller is intentionally empty for now. Endpoints will be added when the Supermarkt
/// use cases are implemented.
/// </remarks>
[ApiController]
[Route("api/supermarkten")]
[Produces("application/json")]
public class SupermarktenController(IMediator mediator) : ControllerBase
{ 
    /// <summary>
    /// Gets a page of supermarkten.
    /// </summary>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The maximum number of items to return.</param>
    /// <returns>The requested verspakket page.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetSupermarkten.Response), StatusCodes.Status200OK)]
    public async Task<GetSupermarkten.Response> Get(int skip = 0, int take = 50)
    {
        return await mediator.SendQueryAsync<GetSupermarkten.Query, GetSupermarkten.Response>(new GetSupermarkten.Query(skip, take));
    }
}
