using Cortex.Mediator;
using Lutra.Application.Verspakketten;
using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Controllers
{
    /// <summary>
    /// Provides read-only access to verspakket resources.
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
    }
}