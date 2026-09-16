using Cortex.Mediator.Commands;

namespace Lutra.Application.Verspakketten;

public sealed partial class EnqueueImportVerspakket
{
    /// <summary>Queues a verspakket import to run in the background.</summary>
    public sealed record Command(string Url) : ICommand<Response>;
}