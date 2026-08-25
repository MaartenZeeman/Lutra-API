using Cortex.Mediator;
using Lutra.API.Requests;
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

    /// <summary>
    /// Creates a new supermarkt.
    /// </summary>
    /// <param name="request">The supermarkt values to create.</param>
    /// <returns>Returns 201 Created with the created supermarkt identifier.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSupermarkt.Response), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateSupermarkt.Response>> Post([FromBody] SupermarktRequest request)
    {
        try
        {
            var command = new CreateSupermarkt.Command(request.Naam);
            var result = await mediator.SendCommandAsync<CreateSupermarkt.Command, CreateSupermarkt.Response>(command);
            return CreatedAtAction(nameof(Get), new { }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing supermarkt with the provided values.
    /// </summary>
    /// <param name="id">The supermarkt identifier.</param>
    /// <param name="request">The updated supermarkt values.</param>
    /// <returns>
    /// Returns 204 No Content when the update succeeds.
    /// Returns 404 Not Found when the specified supermarkt does not exist.
    /// </returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SupermarktRequest request)
    {
        try
        {
            var command = new UpdateSupermarkt.Command(id, request.Naam);
            await mediator.SendCommandAsync<UpdateSupermarkt.Command, UpdateSupermarkt.Response>(command);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith($"Supermarkt with id '{id}'"))
        {
            return NotFound();
        }
    }
}
