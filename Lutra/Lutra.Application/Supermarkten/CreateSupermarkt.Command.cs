using Cortex.Mediator.Commands;

namespace Lutra.Application.Supermarkten;

public sealed partial class CreateSupermarkt
{
    public sealed record Command(string Naam) : ICommand<Response>;
}