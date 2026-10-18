using Cortex.Mediator.Commands;

namespace Lutra.Application.Verspakketten;

public sealed partial class UpdateBeoordeling
{
    public sealed record Command(
        Guid VerspakketId,
        Guid BeoordelingId,
        int CijferSmaak,
        int CijferBereiden,
        bool Aanbevolen,
        string? Tekst) : ICommand<Response>;
}
