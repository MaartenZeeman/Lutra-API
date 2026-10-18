using Cortex.Mediator.Commands;

namespace Lutra.Application.Verspakketten;

public sealed partial class DeleteBeoordeling
{
    public sealed record Command(Guid VerspakketId, Guid BeoordelingId) : ICommand<Response>;
}
