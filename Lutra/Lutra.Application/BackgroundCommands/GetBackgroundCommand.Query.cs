using Cortex.Mediator.Queries;

namespace Lutra.Application.BackgroundCommands;

public sealed partial class GetBackgroundCommand
{
    public sealed record Query(Guid Id) : IQuery<Response?>;
}