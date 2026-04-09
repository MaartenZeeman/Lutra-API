using Cortex.Mediator.Queries;

namespace Lutra.Application.Supermarkten
{
    public sealed partial class GetSupermarkten
    {
        public sealed class Handler : IQueryHandler<Query, Response>
        {
            public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
            {
                return new Response { Supermarkten = [] };
            }
        }
    }
}
