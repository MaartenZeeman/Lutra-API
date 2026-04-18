using Cortex.Mediator.Commands;

namespace Lutra.Application.Verspakketten;

public sealed partial class AddBeoordeling
{
    public sealed record Command(
        Guid VerspakketId,
        int CijferSmaak,
        int CijferBereiden,
        bool Aanbevolen,
        string? Tekst) : ICommand<Response>;
}