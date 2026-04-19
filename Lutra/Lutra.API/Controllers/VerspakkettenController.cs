using Cortex.Mediator;
using Lutra.API.Requests;
using Lutra.Application.Verspakketten;
using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Controllers
{
    /// <summary>
    /// Provides access to verspakket resources.
    /// </summary>
    [ApiController]
    [Route("api/verspakketten")]
    [Produces("application/json")]
    public class VerspakkettenController(IMediator mediator) : ControllerBase
    {
        /// <summary>
        /// Gets a page of verspakketten.
        /// </summary>
        /// <param name="skip">The number of items to skip. Default: 0.</param>
        /// <param name="take">The maximum number of items to return. Default: 50.</param>
        /// <param name="sortField">The field to sort by: Naam, PrijsInCenten, AverageCijferSmaak, or AverageCijferBereiden. Default: Naam.</param>
        /// <param name="sortDirection">The sort direction: Ascending or Descending. Default: Ascending.</param>
        /// <returns>The requested verspakket page.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(GetVerspakketten.Response), StatusCodes.Status200OK)]
        public async Task<GetVerspakketten.Response> Get(
            int skip = 0,
            int take = 50,
            VerspakketSortField sortField = VerspakketSortField.Naam,
            SortDirection sortDirection = SortDirection.Ascending)
        {
            return await mediator.SendQueryAsync<GetVerspakketten.Query, GetVerspakketten.Response>(
                new GetVerspakketten.Query(skip, take, sortField, sortDirection));
        }

        /// <summary>
        /// Gets a specific verspakket by ID.
        /// </summary>
        /// <param name="id">The verspakket ID.</param>
        /// <returns>Returns 200 OK with the verspakket when found, or 404 Not Found when not found.</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GetVerspakket.Response), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GetVerspakket.Response?>> GetById(Guid id)
        {
            var result = await mediator.SendQueryAsync<GetVerspakket.Query, GetVerspakket.Response?>(new GetVerspakket.Query(id));
            if (result?.Verspakket == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Creates a new verspakket.
        /// </summary>
        /// <param name="command">The verspakket values to create.</param>
        /// <returns>Returns 201 Created with the created verspakket identifier.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CreateVerspakket.Response), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateVerspakket.Response>> Post([FromBody] CreateVerspakket.Command command)
        {
            try
            {
                var result = await mediator.SendCommandAsync<CreateVerspakket.Command, CreateVerspakket.Response>(command);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates an existing verspakket with the provided values.
        /// </summary>
        /// <param name="id">The verspakket identifier.</param>
        /// <param name="request">The updated verspakket values.</param>
        /// <returns>
        /// Returns 204 No Content when the update succeeds.
        /// Returns 404 Not Found when the specified verspakket does not exist.
        /// </returns>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVerspakketRequest request)
        {
            try
            {
                var command = new UpdateVerspakket.Command(id, request.Naam, request.PrijsInCenten, request.AantalPersonen, request.SupermarktId);
                await mediator.SendCommandAsync<UpdateVerspakket.Command, UpdateVerspakket.Response>(command);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith($"Verspakket with id '{id}'"))
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Adds a beoordeling to an existing verspakket.
        /// </summary>
        /// <param name="id">The verspakket ID.</param>
        /// <param name="request">The beoordeling values to add.</param>
        /// <returns>Returns 201 Created with the created beoordeling identifier.</returns>
        [HttpPost("{id:guid}/beoordelingen")]
        [ProducesResponseType(typeof(AddBeoordeling.Response), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AddBeoordeling.Response>> AddBeoordeling(Guid id, [FromBody] AddBeoordelingRequest request)
        {
            try
            {
                var command = new AddBeoordeling.Command(id, request.CijferSmaak, request.CijferBereiden, request.Aanbevolen, request.Tekst);
                var result = await mediator.SendCommandAsync<AddBeoordeling.Command, AddBeoordeling.Response>(command);

                return CreatedAtAction(nameof(GetById), new { id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}