using Cortex.Mediator.Commands;

namespace Lutra.Application.Verspakketten;

public sealed partial class DeleteVerspakket
{
    public sealed record Command(Guid Id) : ICommand<Response>;
}
