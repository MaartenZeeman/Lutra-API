using Cortex.Mediator;
using Lutra.Application.Verspakketten;
using Microsoft.AspNetCore.Mvc;

namespace Lutra.API.Controllers
{
    [ApiController]
    [Route("api/verspakketten")]
    public class VerspakkettenController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<GetVerspakketten.Response> GetAll(int skip = 0, int take = 50)
        {
            return await mediator.SendQueryAsync<GetVerspakketten.Query, GetVerspakketten.Response>(new GetVerspakketten.Query(skip, take));
        }
    }
}
