using Cortex.Mediator;
using Lutra.Application.Supermarkten;
using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Controllers;

/// <summary>
/// Provides endpoints for Supermarkt-related operations.
/// </summary>
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
    /// <returns>The requested supermarkt page.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetSupermarkten.Response), StatusCodes.Status200OK)]
    public async Task<GetSupermarkten.Response> Get(int skip = 0, int take = 50)
    {
        return await mediator.SendQueryAsync<GetSupermarkten.Query, GetSupermarkten.Response>(new GetSupermarkten.Query(skip, take));
    }
}
