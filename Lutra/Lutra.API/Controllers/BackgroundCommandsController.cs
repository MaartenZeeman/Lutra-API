using Cortex.Mediator;
using Lutra.Application.BackgroundCommands;
using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Controllers;

/// <summary>
/// Exposes the status of durable background commands.
/// </summary>
[ApiController]
[Route("api/background-commands")]
[Produces("application/json")]
public class BackgroundCommandsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets the current state of a background command job.
    /// </summary>
    /// <param name="id">The background command job ID.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Returns 200 OK with the job state, or 404 Not Found when it does not exist.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetBackgroundCommand.Response), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetBackgroundCommand.Response?>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await mediator.SendQueryAsync<GetBackgroundCommand.Query, GetBackgroundCommand.Response?>(
            new GetBackgroundCommand.Query(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}