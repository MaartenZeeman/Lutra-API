using Cortex.Mediator.Commands;

namespace Lutra.Application.Supermarkten;

public sealed partial class DeleteSupermarkt
{
    public sealed record Command(Guid Id) : ICommand<Response>;
}
