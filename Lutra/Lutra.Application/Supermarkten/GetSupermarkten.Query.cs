using Cortex.Mediator.Queries;

namespace Lutra.Application.Supermarkten;

public sealed partial class GetSupermarkten
{
    public record Query(int Skip, int Take) : IQuery<Response>;
}
