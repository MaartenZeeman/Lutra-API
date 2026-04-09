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
        /// <param name="skip">The number of items to skip.</param>
        /// <param name="take">The maximum number of items to return.</param>
        /// <returns>The requested verspakket page.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(GetVerspakketten.Response), StatusCodes.Status200OK)]
        public async Task<GetVerspakketten.Response> Get(int skip = 0, int take = 50)
        {
            return await mediator.SendQueryAsync<GetVerspakketten.Query, GetVerspakketten.Response>(new GetVerspakketten.Query(skip, take));
        }
    }
}