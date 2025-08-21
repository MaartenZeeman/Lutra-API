using Cortex.Mediator.Queries;

namespace Lutra.Application.Verspakketten
{
    public sealed partial class GetVerspakketten
    {
        public sealed class Handler : IQueryHandler<Query, Response>
        {
            public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
            {
                return new Response { Verspakketten = [] };
            }
        }
    }
}
