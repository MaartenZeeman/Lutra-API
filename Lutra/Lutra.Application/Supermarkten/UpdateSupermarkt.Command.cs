using Cortex.Mediator.Commands;

namespace Lutra.Application.Supermarkten;

public sealed partial class UpdateSupermarkt
{
    public sealed record Command(Guid Id, string Naam) : ICommand<Response>;
}