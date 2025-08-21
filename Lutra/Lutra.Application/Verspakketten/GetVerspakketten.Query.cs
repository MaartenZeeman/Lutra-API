using Cortex.Mediator.Queries;

namespace Lutra.Application.Verspakketten
{
    public sealed partial class GetVerspakketten
    {
        public record Query(int Skip, int Take) : IQuery<Response>;
    }
}
