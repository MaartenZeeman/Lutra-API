using Cortex.Mediator.Queries;

namespace Lutra.Application.Verspakketten
{
    public sealed partial class GetVerspakket
    {
        public record Query(Guid Id) : IQuery<Response?>;
    }
}
